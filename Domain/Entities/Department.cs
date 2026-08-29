using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DepartmentType Type { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Managers { get; set; } = new List<User>();
    public ICollection<ArticleDepartmentStatus> ArticleStatuses { get; set; } = new List<ArticleDepartmentStatus>();
    public ICollection<StitchingTeam> Teams { get; set; } = new List<StitchingTeam>();
}