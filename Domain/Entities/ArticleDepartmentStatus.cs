using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Enums;

namespace Domain.Entities;

public class ArticleDepartmentStatus
{
    public int Id { get; set; }

    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int SequenceNumber { get; set; }
    public string? Note { get; set; }
    public DepartmentStatus Status { get; set; } = DepartmentStatus.Pending;

    public int? InputQuantity { get; set; }
    public int? OutputQuantity { get; set; }
    public int? LossQuantity { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public int? UpdatedByUserId { get; set; }
    public User? UpdatedBy { get; set; }
}