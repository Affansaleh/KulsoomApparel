using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Entities;

namespace Application.Interfaces;

public interface IArticleDepartmentStatusRepository
{
    Task<ArticleDepartmentStatus?> GetByIdAsync(int id);
    Task<List<ArticleDepartmentStatus>> GetByArticleAsync(int articleId);
    Task<List<ArticleDepartmentStatus>> GetByDepartmentAsync(int departmentId);
    Task<ArticleDepartmentStatus?> GetNextPendingAsync(int articleId, int currentSequenceNumber);
    void Update(ArticleDepartmentStatus status);
    Task SaveChangesAsync();
}
