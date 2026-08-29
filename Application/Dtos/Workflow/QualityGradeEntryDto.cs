using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.DTOs.Workflow;

public class QualityGradeEntryDto
{
    public int ArticleId { get; set; }
    public int BGradeQuantity { get; set; }
    public string? Note { get; set; }
    public List<SizeBreakdownEntryDto> SizeBreakdowns { get; set; } = new();
}

public class SizeBreakdownEntryDto
{
    public string SizeLabel { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int Quantity { get; set; }
}