using Application.DTOs.Workflow;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class QualityService : IQualityService
{
    private readonly IArticleRepository _articleRepository;
    private readonly IArticleDepartmentStatusRepository _statusRepository;

    public QualityService(IArticleRepository articleRepository, IArticleDepartmentStatusRepository statusRepository)
    {
        _articleRepository = articleRepository;
        _statusRepository = statusRepository;
    }

    public async Task SubmitGradesAsync(QualityGradeEntryDto dto, int updatedByUserId)
    {
        var article = await _articleRepository.GetByIdAsync(dto.ArticleId);
        if (article == null)
            throw new InvalidOperationException("Article not found.");

        var qualityStatus = article.DepartmentStatuses
            .FirstOrDefault(s => s.Department.Type == DepartmentType.QualityAndPacking);

        if (qualityStatus == null)
            throw new InvalidOperationException("Quality & Packing step not found for this article.");

        if (qualityStatus.Status != DepartmentStatus.InProcess)
            throw new InvalidOperationException("Quality & Packing work has not been started.");

        if (dto.BGradeQuantity < 0 || dto.SizeBreakdowns.Any(x => x.Quantity < 0))
            throw new InvalidOperationException("All quality quantities must be whole numbers greater than or equal to zero.");

        var cuttingSizes = article.CuttingSizeBreakdowns.OrderBy(x => x.OrderIndex).ToList();
        if (cuttingSizes.Count == 0)
            throw new InvalidOperationException("Cutting size breakdown is missing for this article.");
        if (dto.SizeBreakdowns.Count != cuttingSizes.Count)
            throw new InvalidOperationException("Quality size rows must exactly match the Cutting size table.");
        foreach (var cutting in cuttingSizes)
        {
            var quality = dto.SizeBreakdowns.SingleOrDefault(x =>
                string.Equals(x.SizeLabel.Trim(), cutting.SizeLabel.Trim(), StringComparison.OrdinalIgnoreCase));
            if (quality == null)
                throw new InvalidOperationException($"Quality size '{cutting.SizeLabel}' is missing.");
            if (quality.Quantity > cutting.Quantity)
                throw new InvalidOperationException($"A-Grade quantity for size '{cutting.SizeLabel}' cannot exceed its Cutting quantity of {cutting.Quantity}.");
        }

        var aGradeTotal = dto.SizeBreakdowns.Sum(sb => sb.Quantity);
        var input = qualityStatus.InputQuantity ?? 0;
        var maximumBGrade = Math.Max(0, input - aGradeTotal);
        if (dto.BGradeQuantity > maximumBGrade)
            throw new InvalidOperationException($"B-Grade quantity can be at most {maximumBGrade} because A-Grade quantity is {aGradeTotal} and the input quantity is {input}.");

        var totalOutput = aGradeTotal + dto.BGradeQuantity;
        if (totalOutput > input)
            throw new InvalidOperationException($"A-Grade plus B-Grade output cannot exceed the input quantity. Maximum total allowed is {input}.");

        // Replace existing size breakdowns with the new submission.
        article.SizeBreakdowns.Clear();
        foreach (var entry in dto.SizeBreakdowns)
        {
            article.SizeBreakdowns.Add(new ArticleSizeBreakdown
            {
                SizeLabel = entry.SizeLabel,
                OrderIndex = entry.OrderIndex,
                Quantity = entry.Quantity
            });
        }

        article.BGradeQuantity = dto.BGradeQuantity;

        qualityStatus.OutputQuantity = totalOutput;
        qualityStatus.LossQuantity = (qualityStatus.InputQuantity ?? 0) - qualityStatus.OutputQuantity.Value;
        qualityStatus.Note = dto.Note;
        qualityStatus.Status = DepartmentStatus.Done;
        qualityStatus.EndedAt = DateTime.UtcNow;
        qualityStatus.UpdatedByUserId = updatedByUserId;

        _articleRepository.Update(article);
        await _articleRepository.SaveChangesAsync();
    }
}