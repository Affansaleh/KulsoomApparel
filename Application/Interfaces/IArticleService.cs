using Application.DTOs.Article;

namespace Application.Interfaces;

public interface IArticleService
{
    Task<List<ArticleResponseDto>> GetAllAsync();
    Task<ArticleResponseDto?> GetByIdAsync(int id);
    Task<ArticleResponseDto> CreateAsync(ArticleCreateDto dto, int createdByUserId);
    Task UpdateArticleAsync(int articleId, ArticleUpdateDto dto);
    Task TogglePinAsync(int articleId);
}