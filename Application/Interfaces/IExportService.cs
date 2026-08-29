using Application.DTOs.Article;

namespace Application.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportArticlesToExcelAsync(ArticleExportRequestDto request);
}