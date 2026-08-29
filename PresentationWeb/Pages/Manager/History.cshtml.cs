using Application.DTOs.Article;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationWeb.Pages.Manager;

[Authorize(Roles = "Manager")]
public class HistoryModel : PageModel
{
    private readonly IArticleDepartmentStatusRepository _statusRepository;
    private readonly IArticleService _articleService;

    public HistoryModel(
        IArticleDepartmentStatusRepository statusRepository,
        IArticleService articleService)
    {
        _statusRepository = statusRepository;
        _articleService = articleService;
    }

    public class ArticleRow
    {
        public int Id { get; set; }
        public string ArticleCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? Color { get; set; }
        public List<ArticleFabricDto> Fabrics { get; set; } = new();
        public int? Quantity { get; set; }
        public DateTime DeliveryDate { get; set; }
        public bool IsDelivered { get; set; }
        public DateTime? DeptEndedAt { get; set; }
    }

    // Articles that were completed (Done) in the manager's own department.
    public async Task<IActionResult> OnGetArticlesAsync(string? search, string? from, string? to)
    {
        var deptClaim = User.FindFirstValue("DepartmentId");
        if (!int.TryParse(deptClaim, out var deptId))
            return new JsonResult(new List<ArticleRow>());

        var statuses = await _statusRepository.GetByDepartmentAsync(deptId);
        var doneStatuses = statuses
            .Where(s => s.Status == DepartmentStatus.Done)
            .ToList();

        var rows = new List<ArticleRow>();
        foreach (var s in doneStatuses)
        {
            var article = await _articleService.GetByIdAsync(s.ArticleId);
            if (article == null) continue;

            rows.Add(new ArticleRow
            {
                Id = article.Id,
                ArticleCode = article.ArticleCode,
                CompanyName = article.CompanyName,
                Color = article.Color,
                Fabrics = article.Fabrics,
                Quantity = article.Quantity,
                DeliveryDate = article.DeliveryDate,
                IsDelivered = article.IsDelivered,
                DeptEndedAt = s.EndedAt
            });
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            rows = rows.Where(r =>
                r.ArticleCode.ToLower().Contains(q) ||
                r.CompanyName.ToLower().Contains(q)).ToList();
        }

        if (DateTime.TryParse(from, out var fromDate))
            rows = rows.Where(r => r.DeptEndedAt.HasValue && r.DeptEndedAt.Value.Date >= fromDate.Date).ToList();
        if (DateTime.TryParse(to, out var toDate))
            rows = rows.Where(r => r.DeptEndedAt.HasValue && r.DeptEndedAt.Value.Date <= toDate.Date).ToList();

        rows = rows.OrderByDescending(r => r.DeptEndedAt).ToList();
        return new JsonResult(rows);
    }
}
