using Application.DTOs.Fabric;
using System;
using System.Collections.Generic;

namespace Application.DTOs.Article;

public class ArticleCreateDto
{
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

    // Existing fabric links
    public List<ArticleFabricDto> Fabrics { get; set; } = new();

    // New fabrics to create within the same transaction
    public List<NewFabricLinkDto> NewFabricLinks { get; set; } = new();

    public List<int>? DepartmentOrder { get; set; }
}