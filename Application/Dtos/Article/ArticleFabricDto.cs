using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.DTOs.Article;

public class ArticleFabricDto
{
    public int FabricId { get; set; }
    public string? FabricType { get; set; }
    public string? InvNum { get; set; }
    public string? Status { get; set; }
    public string? FabricCode { get; set; }
    public string? Color { get; set; }
    public string? Unit { get; set; }
    public decimal QuantityUsed { get; set; }
}