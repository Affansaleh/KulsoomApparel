namespace Application.DTOs.Workflow;

public class EndDepartmentWorkDto
{
    public int ArticleDepartmentStatusId { get; set; }
    public int? OutputQuantity { get; set; }
    public string? Note { get; set; }
    public string? StitchedBy { get; set; }
    public List<CuttingSizeBreakdownEntryDto> CuttingSizeBreakdowns { get; set; } = new();
}
