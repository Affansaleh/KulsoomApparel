using System;
using System.Collections.Generic;

namespace Application.DTOs.Article;

public class ArticleResponseDto
{
    public int Id { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string ArticleCode { get; set; } = string.Empty;

    public string? Color { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime DeliveryDate { get; set; }

    public string? Season { get; set; }

    public bool EmbellishmentEmbroidery { get; set; }

    public bool EmbellishmentPrinting { get; set; }

    public bool EmbellishmentHandwork { get; set; }

    public int? Quantity { get; set; }

    public decimal? PricePerPiece { get; set; }

    public decimal? PriceTotal { get; set; }

    public int DoneDepartments { get; set; }

    public int TotalDepartments { get; set; }

    public bool IsPinned { get; set; }

    public bool IsDelivered { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public bool CuttingStarted { get; set; }

    public string? AssignedTeamName { get; set; }

    public string? StitchedBy { get; set; }

    public int? BGradeQuantity { get; set; }

    public int AGradeQuantity { get; set; }

    // Article ki total departmental loss.
    public int TotalLossQuantity { get; set; }

    // Existing article-level loss value.
    public int? LossQuantity { get; set; }

    public List<SizeBreakdownEntryDto> SizeBreakdowns { get; set; } = new();

    public string? PackedBy { get; set; }

    public string? CheckedBy { get; set; }

    public int? NoOfCartons { get; set; }

    public string CreatedByUsername { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<string> AlternateCodes { get; set; } = new();

    public List<ArticleFabricDto> Fabrics { get; set; } = new();
}

public class SizeBreakdownEntryDto
{
    public string SizeLabel { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public int Quantity { get; set; }
}