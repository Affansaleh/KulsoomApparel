using Application.DTOs.Fabric;

namespace Application.DTOs.Article;

// A new fabric to create AND link to an article in the same save.
public class NewFabricLinkDto
{
    public FabricCreateDto Fabric { get; set; } = new();
    public decimal QuantityUsed { get; set; }
}