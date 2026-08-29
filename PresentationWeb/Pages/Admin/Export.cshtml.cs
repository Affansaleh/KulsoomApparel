using Application.DTOs.Article;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationWeb.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ExportModel : PageModel
{
    private readonly IArticleService _articleService;
    private readonly IExportService _exportService;

    public ExportModel(
        IArticleService articleService,
        IExportService exportService)
    {
        _articleService = articleService;
        _exportService = exportService;
    }

    // Loads articles for the export selection table.
    public async Task<IActionResult> OnGetArticlesAsync(
        string? from,
        string? to,
        string? search)
    {
        var articles =
            await _articleService.GetAllAsync();

        if (DateTime.TryParse(from, out var fromDate))
        {
            articles = articles
                .Where(article =>
                    article.OrderDate.Date >= fromDate.Date)
                .ToList();
        }

        if (DateTime.TryParse(to, out var toDate))
        {
            articles = articles
                .Where(article =>
                    article.OrderDate.Date <= toDate.Date)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var query =
                search.Trim().ToLowerInvariant();

            articles = articles
                .Where(article =>
                    article.ArticleCode
                        .ToLowerInvariant()
                        .Contains(query)
                    ||
                    article.CompanyName
                        .ToLowerInvariant()
                        .Contains(query))
                .ToList();
        }

        var rows = articles
            .Select(article => new
            {
                article.Id,
                article.ArticleCode,
                article.CompanyName,
                article.OrderDate,
                Status = article.IsDelivered
                    ? "Delivered"
                    : "Active"
            })
            .OrderByDescending(row =>
                row.OrderDate)
            .ToList();

        return new JsonResult(rows);
    }

    // Generates the Excel export.
    public async Task<IActionResult> OnPostGenerateAsync(
        List<int> articleIds,
        bool includeBasic,
        bool includeFabric,
        bool includePricing,
        bool includeDepartments,
        bool includeSizeBreakdown)
    {
        if (articleIds == null ||
            articleIds.Count == 0)
        {
            TempData["ErrorMessage"] =
                "Select at least one article to export.";

            return RedirectToPage("/Admin/Export");
        }

        var columns =
            new List<string>();

        if (includeBasic)
        {
            columns.Add("Basic");
        }

        if (includeFabric)
        {
            columns.Add("Fabric");
        }

        if (includePricing)
        {
            columns.Add("Pricing");
        }

        if (includeDepartments)
        {
            columns.Add("Department");
        }

        if (includeSizeBreakdown)
        {
            columns.Add("Size Breakdown");
        }

        if (columns.Count == 0)
        {
            TempData["ErrorMessage"] =
                "Select at least one export column group.";

            return RedirectToPage("/Admin/Export");
        }

        var request =
            new ArticleExportRequestDto
            {
                ArticleIds = articleIds,
                Columns = columns
            };

        try
        {
            var fileBytes =
                await _exportService
                    .ExportArticlesToExcelAsync(request);

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "articles-export.xlsx");
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;

            return RedirectToPage("/Admin/Export");
        }
    }
}