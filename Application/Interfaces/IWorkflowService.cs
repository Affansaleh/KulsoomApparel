using Application.DTOs.Workflow;

namespace Application.Interfaces;

public interface IWorkflowService
{
    Task<List<ArticleDepartmentStatusDto>> GetByArticleAsync(int articleId);
    Task StartWorkAsync(StartDepartmentWorkDto dto, int updatedByUserId);
    Task EndWorkAsync(EndDepartmentWorkDto dto, int updatedByUserId);
    Task<List<ArticleDepartmentStatusDto>> GetPendingByDepartmentAsync(int departmentId);
    Task UndoLastDepartmentAsync(int articleId, int updatedByUserId);
    Task UndoDeliverAsync(int articleId, int updatedByUserId);
}