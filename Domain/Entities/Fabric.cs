using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Enums;

namespace Domain.Entities;

public class Fabric
{
    public int Id { get; set; }
    public string FabricCode { get; set; } = string.Empty;
    public string? InvNum { get; set; }
    public DateTime? FabricDate { get; set; }
    public string FabricType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public FabricUnit Unit { get; set; } = FabricUnit.Meter;

    public decimal Rate { get; set; }
    public decimal TotalAmount { get; set; }

    public FabricStatus Status { get; set; } = FabricStatus.InStock;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ArticleFabric> ArticleLinks { get; set; } = new List<ArticleFabric>();
}
