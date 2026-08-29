using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities;

public class StatusLog
{
    public int Id { get; set; }

    public int ArticleDepartmentStatusId { get; set; }
    public ArticleDepartmentStatus ArticleDepartmentStatus { get; set; } = null!;

    public DepartmentStatus? OldStatus { get; set; }
    public DepartmentStatus NewStatus { get; set; }

    public int? OutputQuantity { get; set; }
    public int? LossQuantity { get; set; }

    public int ChangedByUserId { get; set; }
    public User ChangedBy { get; set; } = null!;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? Note { get; set; }
}