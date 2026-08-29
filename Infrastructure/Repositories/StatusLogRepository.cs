using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StatusLogRepository : IStatusLogRepository
{
    private readonly ApplicationDbContext _context;

    public StatusLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StatusLog>> GetByArticleDepartmentStatusAsync(int articleDepartmentStatusId)
    {
        return await _context.StatusLogs
            .Where(sl => sl.ArticleDepartmentStatusId == articleDepartmentStatusId)
            .Include(sl => sl.ChangedBy)
            .OrderByDescending(sl => sl.ChangedAt)
            .ToListAsync();
    }

    public async Task<List<StatusLog>> GetByArticleAsync(int articleId)
    {
        return await _context.StatusLogs
            .Where(sl => sl.ArticleDepartmentStatus.ArticleId == articleId)
            .Include(sl => sl.ChangedBy)
            .OrderByDescending(sl => sl.ChangedAt)
            .ToListAsync();
    }

    public async Task AddAsync(StatusLog log)
    {
        await _context.StatusLogs.AddAsync(log);
    }
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}