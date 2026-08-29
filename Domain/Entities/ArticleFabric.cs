using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;

public class ArticleFabric
{
    public int Id { get; set; }

    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;

    public int FabricId { get; set; }
    public Fabric Fabric { get; set; } = null!;

    public decimal QuantityUsed { get; set; }
}
