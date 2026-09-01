using Application.DTOs.Article;
using Application.DTOs.Fabric;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class ArticleService : IArticleService
{
    private readonly IArticleRepository _articleRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IFabricService _fabricService;

    public ArticleService(
        IArticleRepository articleRepository,
        IDepartmentRepository departmentRepository,
        IFabricService fabricService)
    {
        _articleRepository = articleRepository;
        _departmentRepository = departmentRepository;
        _fabricService = fabricService;
    }

    public async Task<List<ArticleResponseDto>> GetAllAsync()
    {
        var articles = await _articleRepository.GetAllActiveAsync();

        return articles
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ArticleResponseDto?> GetByIdAsync(int id)
    {
        var article = await _articleRepository.GetByIdAsync(id);

        return article == null
            ? null
            : MapToDto(article);
    }

    public async Task<ArticleResponseDto> CreateAsync(
        ArticleCreateDto dto,
        int createdByUserId)
    {
        return await _articleRepository.ExecuteInTransactionAsync(
            async () =>
            {
                var codeExists =
                    await _articleRepository.ArticleCodeExistsAsync(
                        dto.ArticleCode);

                if (codeExists)
                {
                    throw new InvalidOperationException(
                        "This article code already exists.");
                }

                var article = new Article
                {
                    CompanyName = dto.CompanyName,
                    ArticleCode = dto.ArticleCode,
                    Color = dto.Color,
                    OrderDate = dto.OrderDate == default
                        ? DateTime.Today
                        : dto.OrderDate,
                    DeliveryDate = dto.DeliveryDate,
                    Season = dto.Season,
                    EmbellishmentEmbroidery =
                        dto.EmbellishmentEmbroidery,
                    EmbellishmentPrinting =
                        dto.EmbellishmentPrinting,
                    EmbellishmentHandwork =
                        dto.EmbellishmentHandwork,
                    Quantity = dto.Quantity,
                    PricePerPiece = dto.PricePerPiece,
                    PriceTotal =
                        dto.PricePerPiece.HasValue &&
                        dto.Quantity.HasValue
                            ? dto.PricePerPiece.Value *
                              dto.Quantity.Value
                            : null,
                    CreatedByUserId = createdByUserId,
                    IsActive = true
                };

                // Create and link new fabrics.
                foreach (var newFabricLink in dto.NewFabricLinks)
                {
                    var createdFabric =
                        await _fabricService.CreateAsync(
                            newFabricLink.Fabric);

                    article.FabricLinks.Add(
                        new ArticleFabric
                        {
                            FabricId = createdFabric.Id,
                            QuantityUsed =
                                newFabricLink.QuantityUsed
                        });

                    await _fabricService.DeductForArticleAsync(
                        createdFabric.Id,
                        newFabricLink.QuantityUsed);
                }

                // Link existing fabrics.
                foreach (var fabric in dto.Fabrics)
                {
                    if (article.FabricLinks.Any(
                        x => x.FabricId == fabric.FabricId))
                    {
                        throw new InvalidOperationException(
                            "Fabric is already linked to this article.");
                    }

                    article.FabricLinks.Add(
                        new ArticleFabric
                        {
                            FabricId = fabric.FabricId,
                            QuantityUsed = fabric.QuantityUsed
                        });

                    await _fabricService.DeductForArticleAsync(
                        fabric.FabricId,
                        fabric.QuantityUsed);
                }

                var allDepartments =
                    await _departmentRepository.GetAllAsync();

                BuildWorkflow(
                    article,
                    dto,
                    allDepartments);

                await _articleRepository.AddAsync(article);
                await _articleRepository.SaveChangesAsync();

                return MapToDto(article);
            });
    }

    private static void BuildWorkflow(
        Article article,
        ArticleCreateDto dto,
        List<Department> allDepartments)
    {
        if (dto.DepartmentOrder is { Count: > 0 })
        {
            var sequence = 1;

            foreach (var departmentId in dto.DepartmentOrder)
            {
                var department =
                    allDepartments.FirstOrDefault(
                        d => d.Id == departmentId);

                if (department == null)
                    continue;

                if (department.Type ==
                    DepartmentType.Printing &&
                    !dto.EmbellishmentPrinting)
                {
                    continue;
                }

                if (department.Type ==
                    DepartmentType.Embroidery &&
                    !dto.EmbellishmentEmbroidery)
                {
                    continue;
                }

                if (department.Type ==
                    DepartmentType.HandWork &&
                    !dto.EmbellishmentHandwork)
                {
                    continue;
                }

                article.DepartmentStatuses.Add(
                    new ArticleDepartmentStatus
                    {
                        DepartmentId = department.Id,
                        SequenceNumber = sequence++,
                        Status = DepartmentStatus.Pending
                    });
            }
        }
        else
        {
            var sequence = 1;

            foreach (var department in allDepartments
                .OrderBy(d => d.OrderIndex))
            {
                var applies = department.Type switch
                {
                    DepartmentType.Printing =>
                        dto.EmbellishmentPrinting,

                    DepartmentType.Embroidery =>
                        dto.EmbellishmentEmbroidery,

                    DepartmentType.HandWork =>
                        dto.EmbellishmentHandwork,

                    _ => true
                };

                if (!applies)
                    continue;

                article.DepartmentStatuses.Add(
                    new ArticleDepartmentStatus
                    {
                        DepartmentId = department.Id,
                        SequenceNumber = sequence++,
                        Status = DepartmentStatus.Pending
                    });
            }
        }
    }

    public async Task UpdateArticleAsync(
        int articleId,
        ArticleUpdateDto dto)
    {
        await _articleRepository.ExecuteInTransactionAsync(
            async () =>
            {
                var article =
                    await _articleRepository.GetByIdAsync(articleId);

                if (article == null)
                {
                    throw new InvalidOperationException(
                        "Article not found.");
                }

                var cuttingStarted =
                    article.DepartmentStatuses.Any(
                        departmentStatus =>
                            departmentStatus.Department != null &&
                            departmentStatus.Department.Type ==
                                DepartmentType.Cutting &&
                            departmentStatus.Status !=
                                DepartmentStatus.Pending);

                article.CompanyName = dto.CompanyName;
                article.Color = dto.Color;
                article.DeliveryDate = dto.DeliveryDate;
                article.Season = dto.Season;
                article.Quantity = dto.Quantity;
                article.PricePerPiece = dto.PricePerPiece;

                article.PriceTotal =
                    dto.PricePerPiece.HasValue &&
                    dto.Quantity.HasValue
                        ? dto.PricePerPiece.Value *
                          dto.Quantity.Value
                        : null;

                article.IsPinned = dto.IsPinned;
                article.StitchedBy = dto.StitchedBy;
                article.UpdatedAt = DateTime.UtcNow;

                // Replace alternate codes.
                var existingCodes =
                    article.AlternateCodes
                        .Select(code => code.Code)
                        .ToList();

                foreach (var oldCode in existingCodes)
                {
                    if (!dto.AlternateCodes.Contains(oldCode))
                    {
                        var codeToRemove =
                            article.AlternateCodes.FirstOrDefault(
                                code => code.Code == oldCode);

                        if (codeToRemove != null)
                        {
                            article.AlternateCodes.Remove(
                                codeToRemove);
                        }
                    }
                }

                foreach (var code in dto.AlternateCodes)
                {
                    if (!article.AlternateCodes.Any(
                        existing => existing.Code == code))
                    {
                        article.AlternateCodes.Add(
                            new ArticleAlternateCode
                            {
                                Code = code
                            });
                    }
                }

                await SyncWorkflowForUpdateAsync(
                    article,
                    dto);

                var desiredFabricLinks =
                    dto.Fabrics.ToList();

                foreach (var newFabricLink in dto.NewFabricLinks)
                {
                    if (cuttingStarted)
                    {
                        throw new InvalidOperationException(
                            "Cannot add a new fabric because Cutting has already started.");
                    }

                    var createdFabric =
                        await _fabricService.CreateAsync(
                            newFabricLink.Fabric);

                    desiredFabricLinks.Add(
                        new ArticleFabricDto
                        {
                            FabricId = createdFabric.Id,
                            QuantityUsed =
                                newFabricLink.QuantityUsed
                        });
                }

                var currentLinks =
                    article.FabricLinks.ToList();

                foreach (var currentLink in currentLinks)
                {
                    var desiredLink =
                        desiredFabricLinks.FirstOrDefault(
                            link => link.FabricId ==
                                    currentLink.FabricId);

                    if (desiredLink == null)
                    {
                        if (cuttingStarted)
                        {
                            throw new InvalidOperationException(
                                "Cannot remove fabric because Cutting has already started.");
                        }

                        await _fabricService.ReturnForArticleAsync(
                            currentLink.FabricId,
                            currentLink.QuantityUsed);

                        article.FabricLinks.Remove(
                            currentLink);
                    }
                    else
                    {
                        var quantityDifference =
                            desiredLink.QuantityUsed -
                            currentLink.QuantityUsed;

                        if (quantityDifference != 0)
                        {
                            if (quantityDifference > 0)
                            {
                                await _fabricService
                                    .DeductForArticleAsync(
                                        currentLink.FabricId,
                                        quantityDifference);
                            }
                            else
                            {
                                await _fabricService
                                    .ReturnForArticleAsync(
                                        currentLink.FabricId,
                                        -quantityDifference);
                            }

                            currentLink.QuantityUsed =
                                desiredLink.QuantityUsed;
                        }
                    }
                }

                foreach (var desiredLink in desiredFabricLinks)
                {
                    if (!article.FabricLinks.Any(
                        link => link.FabricId ==
                                desiredLink.FabricId))
                    {
                        if (cuttingStarted)
                        {
                            throw new InvalidOperationException(
                                "Cannot add fabric because Cutting has already started.");
                        }

                        article.FabricLinks.Add(
                            new ArticleFabric
                            {
                                FabricId =
                                    desiredLink.FabricId,
                                QuantityUsed =
                                    desiredLink.QuantityUsed
                            });

                        await _fabricService
                            .DeductForArticleAsync(
                                desiredLink.FabricId,
                                desiredLink.QuantityUsed);
                    }
                }

                _articleRepository.Update(article);

                await _articleRepository.SaveChangesAsync();
            });
    }

    private async Task SyncWorkflowForUpdateAsync(
        Article article,
        ArticleUpdateDto dto)
    {
        var allDepartments =
            await _departmentRepository.GetAllAsync();

        var embellishmentTypes =
            new[]
            {
                (
                    Type: DepartmentType.Printing,
                    Checked: dto.EmbellishmentPrinting
                ),
                (
                    Type: DepartmentType.Embroidery,
                    Checked: dto.EmbellishmentEmbroidery
                ),
                (
                    Type: DepartmentType.HandWork,
                    Checked: dto.EmbellishmentHandwork
                )
            };

        foreach (var item in embellishmentTypes)
        {
            var existingStatus =
                article.DepartmentStatuses.FirstOrDefault(
                    status =>
                        status.Department != null &&
                        status.Department.Type == item.Type);

            if (item.Checked)
            {
                if (existingStatus == null)
                {
                    var department =
                        allDepartments.FirstOrDefault(
                            d => d.Type == item.Type);

                    if (department == null)
                        continue;

                    var maximumSequence =
                        article.DepartmentStatuses.Any()
                            ? article.DepartmentStatuses.Max(
                                status =>
                                    status.SequenceNumber)
                            : 0;

                    article.DepartmentStatuses.Add(
                        new ArticleDepartmentStatus
                        {
                            DepartmentId = department.Id,
                            SequenceNumber =
                                maximumSequence + 1,
                            Status = DepartmentStatus.Pending
                        });
                }
            }
            else
            {
                if (existingStatus != null &&
                    existingStatus.Status ==
                        DepartmentStatus.Pending)
                {
                    article.DepartmentStatuses.Remove(
                        existingStatus);
                }
                else if (existingStatus != null)
                {
                    throw new InvalidOperationException(
                        $"Cannot remove '{existingStatus.Department?.Name ?? "department"}' from workflow because its work has already started or completed.");
                }
            }
        }
    }

    public async Task TogglePinAsync(int articleId)
    {
        var article =
            await _articleRepository.GetByIdAsync(articleId);

        if (article == null)
        {
            throw new InvalidOperationException(
                "Article not found.");
        }

        if (article.IsPinned)
        {
            article.IsPinned = false;
        }
        else
        {
            var pinned =
                await _articleRepository.GetPinnedAsync();

            if (pinned.Count >= 5)
            {
                throw new InvalidOperationException(
                    "Maximum 5 pinned articles.");
            }

            article.IsPinned = true;
        }

        article.UpdatedAt = DateTime.UtcNow;

        _articleRepository.Update(article);

        await _articleRepository.SaveChangesAsync();
    }

    private static ArticleResponseDto MapToDto(Article article)
    {
        var aGrade =
            article.SizeBreakdowns.Sum(
                size => size.Quantity);

        var bGrade =
            article.BGradeQuantity ?? 0;

        var totalDepartmentLoss =
            article.DepartmentStatuses.Sum(
                status => status.LossQuantity ?? 0);

        return new ArticleResponseDto
        {
            Id = article.Id,
            CompanyName = article.CompanyName,
            ArticleCode = article.ArticleCode,
            Color = article.Color,
            OrderDate = article.OrderDate,
            DeliveryDate = article.DeliveryDate,
            Season = article.Season,
            EmbellishmentEmbroidery =
                article.EmbellishmentEmbroidery,
            EmbellishmentPrinting =
                article.EmbellishmentPrinting,
            EmbellishmentHandwork =
                article.EmbellishmentHandwork,
            Quantity = article.Quantity,
            PricePerPiece = article.PricePerPiece,
            PriceTotal = article.PriceTotal,
            IsPinned = article.IsPinned,
            DoneDepartments =
                article.DepartmentStatuses.Count(
                    status =>
                        status.Status ==
                        DepartmentStatus.Done),
            TotalDepartments =
                article.DepartmentStatuses.Count,
            IsDelivered = article.IsDelivered,
            DeliveredAt = article.DeliveredAt,
            CuttingStarted =
                article.DepartmentStatuses.Any(
                    status =>
                        status.Department != null &&
                        status.Department.Type ==
                            DepartmentType.Cutting &&
                        status.Status !=
                            DepartmentStatus.Pending),
            AssignedTeamName =
                article.AssignedTeam?.Name,
            StitchedBy = article.StitchedBy,
            BGradeQuantity = article.BGradeQuantity,
            AGradeQuantity = aGrade,
            TotalLossQuantity = totalDepartmentLoss,
            LossQuantity =
                article.Quantity.HasValue
                    ? article.Quantity.Value -
                      aGrade -
                      bGrade
                    : null,
            SizeBreakdowns =
                article.SizeBreakdowns
                    .OrderBy(size => size.OrderIndex)
                    .Select(size =>
                        new SizeBreakdownEntryDto
                        {
                            SizeLabel = size.SizeLabel,
                            OrderIndex = size.OrderIndex,
                            Quantity = size.Quantity
                        })
                    .ToList(),
            CuttingSizeBreakdowns = article.CuttingSizeBreakdowns
                .OrderBy(size => size.OrderIndex)
                .Select(size => new SizeBreakdownEntryDto
                {
                    SizeLabel = size.SizeLabel,
                    OrderIndex = size.OrderIndex,
                    Quantity = size.Quantity
                }).ToList(),
            CuttingSizeTotal = article.CuttingSizeBreakdowns.Sum(x => x.Quantity),
            PreQualityLossQuantity = Math.Max(0,
                article.CuttingSizeBreakdowns.Sum(x => x.Quantity) -
                (article.DepartmentStatuses.FirstOrDefault(x => x.Department.Type == DepartmentType.QualityAndPacking)?.InputQuantity ?? article.CuttingSizeBreakdowns.Sum(x => x.Quantity))),
            CGradeQuantity = Math.Max(0,
                (article.DepartmentStatuses.FirstOrDefault(x => x.Department.Type == DepartmentType.QualityAndPacking)?.InputQuantity ?? 0) - aGrade - bGrade),
            PackedBy = article.PackedBy,
            CheckedBy = article.CheckedBy,
            NoOfCartons = article.NoOfCartons,
            CreatedByUsername =
                article.CreatedBy?.Username ??
                string.Empty,
            CreatedAt = article.CreatedAt,
            AlternateCodes =
                article.AlternateCodes
                    .Select(code => code.Code)
                    .ToList(),
            Fabrics =
                article.FabricLinks
                    .Select(link =>
                        new ArticleFabricDto
                        {
                            FabricId = link.FabricId,
                            FabricCode =
                                link.Fabric?.FabricCode,
                            QuantityUsed =
                                link.QuantityUsed,
                            FabricType =
                                link.Fabric?.FabricType,
                            InvNum =
                                link.Fabric?.InvNum,
                            Status =
                                link.Fabric?.Status
                                    .ToString()
                        })
                    .ToList()
        };
    }
}