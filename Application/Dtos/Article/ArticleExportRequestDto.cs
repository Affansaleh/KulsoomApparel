namespace Application.DTOs.Article;

public class ArticleExportRequestDto
{
    public List<int> ArticleIds { get; set; } = new();
    public List<string>? Columns { get; set; }   // optional - if null/empty, export all default columns
}