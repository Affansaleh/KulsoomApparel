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

public class ArticleRepository : IArticleRepository
{
    private readonly ApplicationDbContext _context;

    public ArticleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Article?> GetByIdAsync(int id)
    {
        return await _context.Articles
            .Include(a => a.CreatedBy)
            .Include(a => a.AssignedTeam)
            .Include(a => a.FabricLinks).ThenInclude(fl => fl.Fabric)
            .Include(a => a.AlternateCodes)
            .Include(a => a.SizeBreakdowns)
            .Include(a => a.DepartmentStatuses).ThenInclude(ds => ds.Department)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Article?> GetByCodeAsync(string articleCode)
    {
        return await _context.Articles
            .FirstOrDefaultAsync(a => a.ArticleCode == articleCode);
    }

    public async Task<List<Article>> GetAllActiveAsync()
    {
        return await _context.Articles
        .Where(a => a.IsActive)
        .Include(a => a.AssignedTeam)
        .Include(a => a.FabricLinks).ThenInclude(fl => fl.Fabric)
        .Include(a => a.AlternateCodes)
        .Include(a => a.DepartmentStatuses).ThenInclude(ds => ds.Department)
        .OrderByDescending(a => a.CreatedAt)
        .ToListAsync();
    }

    public async Task<List<Article>> GetPinnedAsync()
    {
        return await _context.Articles
            .Where(a => a.IsActive && a.IsPinned)
            .ToListAsync();
    }

    public async Task<bool> ArticleCodeExistsAsync(string articleCode)
    {
        return await _context.Articles.AnyAsync(a => a.ArticleCode == articleCode);
    }

    public async Task<bool> AlternateCodeExistsAsync(string code)
    {
        return await _context.ArticleAlternateCodes.AnyAsync(ac => ac.Code == code);
    }

    public async Task AddAsync(Article article)
    {
        await _context.Articles.AddAsync(article);
    }

    public void Update(Article article)
    {
        _context.Articles.Update(article);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
    public async Task<List<Article>> GetAllActiveWithDetailsAsync()
    {
        return await _context.Articles
            .Where(a => a.IsActive)
            .Include(a => a.FabricLinks).ThenInclude(fl => fl.Fabric)
            .Include(a => a.DepartmentStatuses).ThenInclude(ds => ds.Department)
            .Include(a => a.SizeBreakdowns)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await action();
            await tx.CommitAsync();
            return result;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            await action();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}