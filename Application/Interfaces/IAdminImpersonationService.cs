using Application.DTOs.Workflow;

namespace Application.Interfaces;

public interface IAdminImpersonationService
{
    Task<List<ArticleDepartmentStatusDto>> ViewDepartmentAsync(int departmentId);
}