using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces;

public interface IStatusLogRepository
{
    Task<List<StatusLog>> GetByArticleDepartmentStatusAsync(int articleDepartmentStatusId);
    Task<List<StatusLog>> GetByArticleAsync(int articleId);
    Task AddAsync(StatusLog log);
    Task SaveChangesAsync();
}