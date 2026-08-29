using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Entities;

public class Article
{
    public int Id { get; set; }

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
    public decimal? PriceTotal { get; set; }

    public bool IsPinned { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? AssignedTeamId { get; set; }
    public StitchingTeam? AssignedTeam { get; set; }

    public int? BGradeQuantity { get; set; }

    public string? StitchedBy { get; set; }

    public string? PackedBy { get; set; }
    public string? CheckedBy { get; set; }
    public int? NoOfCartons { get; set; }
    public string? ReceivedBy { get; set; }
    public DateTime? ReceivedDate { get; set; }

    public bool IsDelivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ArticleFabric> FabricLinks { get; set; } = new List<ArticleFabric>();
    public ICollection<ArticleAlternateCode> AlternateCodes { get; set; } = new List<ArticleAlternateCode>();
    public ICollection<ArticleSizeBreakdown> SizeBreakdowns { get; set; } = new List<ArticleSizeBreakdown>();
    public ICollection<ArticleDepartmentStatus> DepartmentStatuses { get; set; } = new List<ArticleDepartmentStatus>();
}