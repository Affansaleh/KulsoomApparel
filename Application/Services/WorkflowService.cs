using Application.DTOs.Workflow;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IArticleDepartmentStatusRepository _statusRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IFabricRepository _fabricRepository;
    private readonly IStatusLogRepository _statusLogRepository;

    public WorkflowService(
        IArticleDepartmentStatusRepository statusRepository,
        IArticleRepository articleRepository,
        IFabricRepository fabricRepository,
        IStatusLogRepository statusLogRepository)
    {
        _statusRepository = statusRepository;
        _articleRepository = articleRepository;
        _fabricRepository = fabricRepository;
        _statusLogRepository = statusLogRepository;
    }

    public async Task<List<ArticleDepartmentStatusDto>> GetByArticleAsync(int articleId)
    {
        var statuses = await _statusRepository.GetByArticleAsync(articleId);
        return statuses.Select(MapToDto).ToList();
    }

    public async Task StartWorkAsync(StartDepartmentWorkDto dto, int updatedByUserId)
    {
        var status = await _statusRepository.GetByIdAsync(dto.ArticleDepartmentStatusId);
        if (status == null)
            throw new InvalidOperationException("Workflow record not found.");

        if (status.Status != DepartmentStatus.Pending)
            throw new InvalidOperationException("This department's work has already been started or completed.");

        // Rule: the previous department (by SequenceNumber) must be Done before this one can start.
        var allForArticle = await _statusRepository.GetByArticleAsync(status.ArticleId);
        var previous = allForArticle
            .Where(s => s.SequenceNumber < status.SequenceNumber)
            .OrderByDescending(s => s.SequenceNumber)
            .FirstOrDefault();

        if (previous != null && previous.Status != DepartmentStatus.Done)
            throw new InvalidOperationException($"Cannot start - previous department '{previous.Department.Name}' is not finished yet.");

        // Rule: Cutting requires at least one fabric linked to the article.
        if (status.Department.Type == DepartmentType.Cutting)
        {
            var article = await _articleRepository.GetByIdAsync(status.ArticleId);
            if (article == null || !article.FabricLinks.Any())
                throw new InvalidOperationException("Cannot start Cutting - no fabric has been added to this article yet.");
        }

        // Rule: Stitching requires a team assignment.
        if (status.Department.Type == DepartmentType.Stitching)
        {
            if (dto.AssignedTeamId == null)
                throw new InvalidOperationException("Please assign Team A or Team B before starting Stitching.");

            var article = await _articleRepository.GetByIdAsync(status.ArticleId);
            article!.AssignedTeamId = dto.AssignedTeamId;
            _articleRepository.Update(article);
        }


        if (previous != null)
        {
            status.InputQuantity = previous.OutputQuantity;
        }
        else
        {
            var article = await _articleRepository.GetByIdAsync(status.ArticleId);
            status.InputQuantity = article?.Quantity;
        }

        status.Status = DepartmentStatus.InProcess;
        status.StartedAt = DateTime.UtcNow;
        status.UpdatedByUserId = updatedByUserId;

        _statusRepository.Update(status);
        await _statusRepository.SaveChangesAsync();

        await LogStatusChangeAsync(status, DepartmentStatus.Pending, updatedByUserId, "Work started.");
    }

    public async Task EndWorkAsync(EndDepartmentWorkDto dto, int updatedByUserId)
    {
        var status = await _statusRepository.GetByIdAsync(dto.ArticleDepartmentStatusId);
        if (status == null)
            throw new InvalidOperationException("Workflow record not found.");

        if (status.Status != DepartmentStatus.InProcess)
            throw new InvalidOperationException("This department's work has not been started yet.");

        // Sampling submission waits for the Pattern manager's decision. It is not final Done yet.
        if (status.Department.Type == DepartmentType.Sampling)
        {
            if (status.SamplingApprovalState == "AwaitingApproval")
                throw new InvalidOperationException("This Sampling attempt is already waiting for Pattern approval.");

            status.SamplingAttemptCount++;
            status.SamplingApprovalState = "AwaitingApproval";
            status.SamplingSubmittedAt = DateTime.UtcNow;
            status.EndedAt = status.SamplingSubmittedAt;
            status.OutputQuantity = status.InputQuantity;
            status.LossQuantity = 0;
            status.Note = dto.Note;
            status.UpdatedByUserId = updatedByUserId;
            _statusRepository.Update(status);
            await _statusRepository.SaveChangesAsync();
            await LogStatusChangeAsync(status, DepartmentStatus.InProcess, updatedByUserId,
                $"Sampling attempt {status.SamplingAttemptCount} submitted for Pattern approval.");
            return;
        }

        // Pattern doesn't track quantity - just carries the input quantity forward.
        bool isQuantityless = status.Department.Type == DepartmentType.Pattern;

        if (isQuantityless)
        {
            status.OutputQuantity = status.InputQuantity;
            status.LossQuantity = 0;
        }
        else if (status.Department.Type == DepartmentType.Cutting)
        {
            if (dto.CuttingSizeBreakdowns.Count == 0)
                throw new InvalidOperationException("At least one Cutting size quantity is required.");
            if (dto.CuttingSizeBreakdowns.Any(x => string.IsNullOrWhiteSpace(x.SizeLabel) || x.Quantity < 0))
                throw new InvalidOperationException("Cutting sizes and quantities are invalid.");
            if (dto.CuttingSizeBreakdowns.GroupBy(x => x.SizeLabel.Trim(), StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Duplicate Cutting sizes are not allowed.");

            var article = await _articleRepository.GetByIdAsync(status.ArticleId)
                ?? throw new InvalidOperationException("Article not found.");
            article.CuttingSizeBreakdowns.Clear();
            foreach (var entry in dto.CuttingSizeBreakdowns.OrderBy(x => x.OrderIndex))
                article.CuttingSizeBreakdowns.Add(new ArticleCuttingSizeBreakdown
                {
                    SizeLabel = entry.SizeLabel.Trim(),
                    OrderIndex = entry.OrderIndex,
                    Quantity = entry.Quantity
                });

            status.OutputQuantity = dto.CuttingSizeBreakdowns.Sum(x => x.Quantity);
            status.LossQuantity = status.InputQuantity.HasValue
                ? Math.Max(0, status.InputQuantity.Value - status.OutputQuantity.Value)
                : 0;
            _articleRepository.Update(article);
        }
        else
        {
            if (dto.OutputQuantity == null)
                throw new InvalidOperationException("Output quantity is required to end work for this department.");
            if (dto.OutputQuantity.Value > (status.InputQuantity ?? 0))
                throw new InvalidOperationException($"Output quantity cannot exceed the input quantity. Maximum allowed is {status.InputQuantity ?? 0}.");

            status.OutputQuantity = dto.OutputQuantity;
            status.LossQuantity = (status.InputQuantity ?? 0) - dto.OutputQuantity.Value;
        }

        if (status.Department.Type == DepartmentType.Stitching)
        {
            if (string.IsNullOrWhiteSpace(dto.StitchedBy))
                throw new InvalidOperationException("Stitched By (Name) is required to end Stitching.");

            var article = await _articleRepository.GetByIdAsync(status.ArticleId);
            if (article == null) throw new InvalidOperationException("Article not found.");
            article.StitchedBy = dto.StitchedBy.Trim();
            _articleRepository.Update(article);
        }

        status.Note = dto.Note;
        status.Status = DepartmentStatus.Done;
        status.EndedAt = DateTime.UtcNow;
        status.UpdatedByUserId = updatedByUserId;

        _statusRepository.Update(status);
        await _statusRepository.SaveChangesAsync();

        await LogStatusChangeAsync(status, DepartmentStatus.InProcess, updatedByUserId, dto.Note);
    }

    private async Task LogStatusChangeAsync(ArticleDepartmentStatus status, DepartmentStatus oldStatus, int userId, string? note)
    {
        var log = new StatusLog
        {
            ArticleDepartmentStatusId = status.Id,
            OldStatus = oldStatus,
            NewStatus = status.Status,
            OutputQuantity = status.OutputQuantity,
            LossQuantity = status.LossQuantity,
            ChangedByUserId = userId,
            Note = note
        };

        await _statusLogRepository.AddAsync(log);
        await _statusLogRepository.SaveChangesAsync();
    }

    private static ArticleDepartmentStatusDto MapToDto(ArticleDepartmentStatus s) => new()
    {
        Id = s.Id,
        ArticleId = s.ArticleId,
        DepartmentId = s.DepartmentId,
        DepartmentName = s.Department.Name,
        SequenceNumber = s.SequenceNumber,
        Status = s.Status.ToString(),
        InputQuantity = s.InputQuantity,
        OutputQuantity = s.OutputQuantity,
        LossQuantity = s.LossQuantity,
        Note = s.Note,
        StartedAt = s.StartedAt,
        EndedAt = s.EndedAt,
        DurationDisplay = (s.StartedAt.HasValue && s.EndedAt.HasValue)
            ? $"{(int)(s.EndedAt.Value - s.StartedAt.Value).TotalHours}h {(s.EndedAt.Value - s.StartedAt.Value).Minutes}m"
            : null,
        UpdatedByUsername = null,   // populate if needed via a User lookup
        SamplingAttemptCount = s.SamplingAttemptCount,
        SamplingApprovalState = s.SamplingApprovalState,
        SamplingSubmittedAt = s.SamplingSubmittedAt,
        SamplingReviewedAt = s.SamplingReviewedAt,
        SamplingReviewNote = s.SamplingReviewNote
    };

    public async Task<List<ArticleDepartmentStatusDto>> GetPendingByDepartmentAsync(int departmentId)
    {
        var statuses = await _statusRepository.GetByDepartmentAsync(departmentId);

        var relevant = statuses
            .Where(s => s.Status != DepartmentStatus.Done && !s.Article.IsDelivered &&
                        !(s.Department.Type == DepartmentType.Sampling && s.SamplingApprovalState == "AwaitingApproval"))
            .ToList();

        var result = new List<ArticleDepartmentStatusDto>();

        foreach (var s in relevant)
        {
            var dto = MapToDto(s);
            dto.ArticleCode = s.Article.ArticleCode;
            dto.CompanyName = s.Article.CompanyName;
            dto.DeliveryDate = s.Article.DeliveryDate;
            dto.IsPinned = s.Article.IsPinned;

            if (s.Status == DepartmentStatus.Pending)
            {
                var allForArticle = await _statusRepository.GetByArticleAsync(s.ArticleId);
                var previous = allForArticle.Where(x => x.SequenceNumber < s.SequenceNumber);
                dto.CanStart = previous.All(x => x.Status == DepartmentStatus.Done);
            }
            else
            {
                dto.CanStart = true;
            }

            result.Add(dto);
        }

        return result
            .OrderByDescending(r => r.IsPinned)
            .ThenBy(r => r.DeliveryDate)
            .ToList();
    }


    public async Task<List<ArticleDepartmentStatusDto>> GetSamplingAwaitingApprovalAsync()
    {
        var statuses = await _statusRepository.GetSamplingAwaitingApprovalAsync();
        return statuses.Select(s =>
        {
            var dto = MapToDto(s);
            dto.ArticleCode = s.Article.ArticleCode;
            dto.CompanyName = s.Article.CompanyName;
            dto.DeliveryDate = s.Article.DeliveryDate;
            dto.IsPinned = s.Article.IsPinned;
            return dto;
        }).OrderByDescending(x => x.IsPinned).ThenBy(x => x.SamplingSubmittedAt).ToList();
    }

    public async Task ReviewSamplingAsync(int statusId, bool approved, string? note, int reviewedByUserId)
    {
        var status = await _statusRepository.GetByIdAsync(statusId)
            ?? throw new InvalidOperationException("Sampling workflow record not found.");
        if (status.Department.Type != DepartmentType.Sampling || status.SamplingApprovalState != "AwaitingApproval")
            throw new InvalidOperationException("This Sampling attempt is not waiting for approval.");

        status.SamplingReviewedAt = DateTime.UtcNow;
        status.SamplingReviewedByUserId = reviewedByUserId;
        status.SamplingReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        status.UpdatedByUserId = reviewedByUserId;

        if (approved)
        {
            status.SamplingApprovalState = "Approved";
            status.Status = DepartmentStatus.Done;
            // EndedAt remains the Sampling manager's final submission time.
        }
        else
        {
            status.SamplingApprovalState = "Rejected";
            status.Status = DepartmentStatus.InProcess;
            status.StartedAt = DateTime.UtcNow;
            status.EndedAt = null;
            status.OutputQuantity = null;
            status.LossQuantity = null;
        }

        _statusRepository.Update(status);
        await _statusRepository.SaveChangesAsync();
        await LogStatusChangeAsync(status, DepartmentStatus.InProcess, reviewedByUserId,
            approved ? "Sampling approved by Pattern." : "Sampling rejected by Pattern; resampling requested.");
    }

    public async Task UndoLastDepartmentAsync(int articleId, int updatedByUserId)
    {
        var statuses = await _statusRepository.GetByArticleAsync(articleId);
        if (statuses.Count == 0)
            throw new InvalidOperationException("No department workflow defined for this article.");

        // If article auto-delivered, un-deliver first (delivery is a layer on top).
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article != null && article.IsDelivered)
        {
            article.IsDelivered = false;
            article.DeliveredAt = null;
            _articleRepository.Update(article);
        }

        // Case 1: a department is currently InProcess → revert just it to Pending.
        var inProcess = statuses.FirstOrDefault(s => s.Status == DepartmentStatus.InProcess);
        if (inProcess != null)
        {
            inProcess.Status = DepartmentStatus.Pending;
            inProcess.StartedAt = null;
            inProcess.InputQuantity = null;
            inProcess.UpdatedByUserId = updatedByUserId;
            _statusRepository.Update(inProcess);

            await _statusRepository.SaveChangesAsync();
            await LogStatusChangeAsync(inProcess, DepartmentStatus.InProcess, updatedByUserId,
                $"Undo: {inProcess.Department?.Name ?? "department"} InProcess -> Pending");
            return;
        }

        // Case 2: nothing InProcess → reopen highest-SequenceNumber Done to InProcess.
        var lastDone = statuses
            .Where(s => s.Status == DepartmentStatus.Done)
            .OrderByDescending(s => s.SequenceNumber)
            .FirstOrDefault();

        if (lastDone != null)
        {
            lastDone.Status = DepartmentStatus.InProcess;
            lastDone.EndedAt = null;
            lastDone.OutputQuantity = null;
            lastDone.LossQuantity = null;
            lastDone.UpdatedByUserId = updatedByUserId;
            _statusRepository.Update(lastDone);

            await _statusRepository.SaveChangesAsync();
            await LogStatusChangeAsync(lastDone, DepartmentStatus.Done, updatedByUserId,
                $"Undo: {lastDone.Department?.Name ?? "department"} Done -> InProcess");
            return;
        }

        throw new InvalidOperationException("Nothing to undo for this article.");
    }
    public async Task UndoDeliverAsync(int articleId, int updatedByUserId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
            throw new InvalidOperationException("Article not found.");

        if (!article.IsDelivered)
            throw new InvalidOperationException("This article is not delivered.");

        article.IsDelivered = false;
        article.DeliveredAt = null;
        _articleRepository.Update(article);

        // Reopen the last Done department (delivery) back to InProcess — one step.
        var statuses = await _statusRepository.GetByArticleAsync(articleId);
        var lastDone = statuses
            .Where(s => s.Status == DepartmentStatus.Done)
            .OrderByDescending(s => s.SequenceNumber)
            .FirstOrDefault();

        if (lastDone != null)
        {
            lastDone.Status = DepartmentStatus.InProcess;
            lastDone.EndedAt = null;
            lastDone.OutputQuantity = null;
            lastDone.LossQuantity = null;
            lastDone.UpdatedByUserId = updatedByUserId;
            _statusRepository.Update(lastDone);
        }

        await _statusRepository.SaveChangesAsync();
    }
}