namespace Domain.Entities;

public class ArticleCuttingSizeBreakdown
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public string SizeLabel { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int Quantity { get; set; }
}
