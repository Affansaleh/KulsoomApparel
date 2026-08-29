namespace Application.DTOs.Fabric;

public class FabricCreateDto
{
    public string FabricCode { get; set; } = string.Empty;
    public string? InvNum { get; set; }
    public DateTime? FabricDate { get; set; }
    public string FabricType { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? Rate { get; set; }
}