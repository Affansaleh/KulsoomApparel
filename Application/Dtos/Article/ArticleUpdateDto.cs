using System.Collections.Generic;

namespace Application.DTOs.Article;

public class ArticleUpdateDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string? Season { get; set; }
    public int? Quantity { get; set; }
    public decimal? PricePerPiece { get; set; }
    public bool IsPinned { get; set; }
    public string? StitchedBy { get; set; }

    public bool EmbellishmentEmbroidery { get; set; }
    public bool EmbellishmentPrinting { get; set; }
    public bool EmbellishmentHandwork { get; set; }
    public List<string> AlternateCodes { get; set; } = new();
    public List<ArticleFabricDto> Fabrics { get; set; } = new();
    public List<NewFabricLinkDto> NewFabricLinks { get; set; } = new();
}
