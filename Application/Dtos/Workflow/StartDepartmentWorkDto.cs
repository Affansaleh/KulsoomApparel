using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Workflow;
public class StartDepartmentWorkDto
{
    public int ArticleDepartmentStatusId { get; set; }
    public int? AssignedTeamId { get; set; }
}