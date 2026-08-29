using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Workflow;

public class DeliveryConfirmDto
{
    public int ArticleId { get; set; }
    public string? PackedBy { get; set; }
    public string? CheckedBy { get; set; }
    public int? NoOfCartons { get; set; }
    public string? Note { get; set; }
}
