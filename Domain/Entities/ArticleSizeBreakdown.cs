using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;

public class ArticleSizeBreakdown
{
    public int Id { get; set; }

    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public string SizeLabel { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int Quantity { get; set; }
}