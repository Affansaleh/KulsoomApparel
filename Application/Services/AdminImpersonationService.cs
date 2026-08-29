using Application.DTOs.Workflow;
using Application.Interfaces;

namespace Application.Services;

public class AdminImpersonationService : IAdminImpersonationService
{
    private readonly IArticleDepartmentStatusRepository _statusRepository;

    public AdminImpersonationService(IArticleDepartmentStatusRepository statusRepository)
    {
        _statusRepository = statusRepository;
    }

    public async Task<List<ArticleDepartmentStatusDto>> ViewDepartmentAsync(int departmentId)
    {
        var statuses = await _statusRepository.GetByDepartmentAsync(departmentId);

        return statuses.Select(s => new ArticleDepartmentStatusDto
        {
            Id = s.Id,
            ArticleId = s.ArticleId,
            DepartmentId = s.DepartmentId,
            SequenceNumber = s.SequenceNumber,
            Status = s.Status.ToString(),
            InputQuantity = s.InputQuantity,
            OutputQuantity = s.OutputQuantity,
            LossQuantity = s.LossQuantity,
            Note = s.Note,
            StartedAt = s.StartedAt,
            EndedAt = s.EndedAt
        }).ToList();
    }
}