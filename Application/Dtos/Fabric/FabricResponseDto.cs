using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Fabric;

public class FabricResponseDto
{
    public int Id { get; set; }
    public string FabricCode { get; set; } = string.Empty;
    public string? InvNum { get; set; }
    public DateTime? FabricDate { get; set; }
    public string FabricType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
