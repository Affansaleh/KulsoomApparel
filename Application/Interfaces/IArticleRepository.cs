using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(int id);
    Task<Article?> GetByCodeAsync(string articleCode);
    Task<List<Article>> GetAllActiveAsync();
    Task<List<Article>> GetPinnedAsync();
    Task<bool> ArticleCodeExistsAsync(string articleCode);
    Task<bool> AlternateCodeExistsAsync(string code);
    Task AddAsync(Article article);
    void Update(Article article);
    Task SaveChangesAsync();
    Task<List<Article>> GetAllActiveWithDetailsAsync();
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action);
    Task ExecuteInTransactionAsync(Func<Task> action);
}