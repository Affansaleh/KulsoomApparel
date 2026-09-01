using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Infrastructure.AppDbContext;

namespace Infrastructure.Repositories;

public class ArticleDepartmentStatusRepository : IArticleDepartmentStatusRepository
{
    private readonly ApplicationDbContext _context;

    public ArticleDepartmentStatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ArticleDepartmentStatus?> GetByIdAsync(int id)
    {
        return await _context.ArticleDepartmentStatuses
            .Include(ads => ads.Department)
            .Include(ads => ads.Article)
            .FirstOrDefaultAsync(ads => ads.Id == id);
    }

    public async Task<List<ArticleDepartmentStatus>> GetByArticleAsync(int articleId)
    {
        return await _context.ArticleDepartmentStatuses
            .Where(ads => ads.ArticleId == articleId)
            .Include(ads => ads.Department)
            .OrderBy(ads => ads.SequenceNumber)
            .ToListAsync();
    }

    public async Task<List<ArticleDepartmentStatus>> GetByDepartmentAsync(int departmentId)
    {
        return await _context.ArticleDepartmentStatuses
            .Where(ads => ads.DepartmentId == departmentId)
            .Include(ads => ads.Article)
            .ToListAsync();
    }

    public async Task<ArticleDepartmentStatus?> GetNextPendingAsync(int articleId, int currentSequenceNumber)
    {
        return await _context.ArticleDepartmentStatuses
            .Where(ads => ads.ArticleId == articleId && ads.SequenceNumber > currentSequenceNumber)
            .OrderBy(ads => ads.SequenceNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ArticleDepartmentStatus>> GetSamplingAwaitingApprovalAsync()
    {
        return await _context.ArticleDepartmentStatuses
            .Where(x => x.Department.Type == Domain.Enums.DepartmentType.Sampling &&
                        x.SamplingApprovalState == "AwaitingApproval" && !x.Article.IsDelivered)
            .Include(x => x.Department)
            .Include(x => x.Article)
            .OrderBy(x => x.SamplingSubmittedAt)
            .ToListAsync();
    }

    public void Update(ArticleDepartmentStatus status)
    {
        _context.ArticleDepartmentStatuses.Update(status);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}