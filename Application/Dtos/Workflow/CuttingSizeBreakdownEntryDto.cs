namespace Application.DTOs.Workflow;

public class CuttingSizeBreakdownEntryDto
{
    public string SizeLabel { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int Quantity { get; set; }
}
