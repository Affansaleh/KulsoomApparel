using Application.DTOs.Article;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationWeb.Pages.Admin;

[Authorize(Roles = "Admin")]
public class HistoryModel : PageModel
{
    private readonly IArticleService _articleService;
    private readonly IWorkflowService _workflowService;

    public HistoryModel(IArticleService articleService, IWorkflowService workflowService)
    {
        _articleService = articleService;
        _workflowService = workflowService;
    }

    // Delivered articles, live-searchable by code/company + optional delivery-date range.
    public async Task<IActionResult> OnGetArticlesAsync(string? search, string? from, string? to)
    {
        var articles = await _articleService.GetAllAsync();
        articles = articles.Where(a => a.IsDelivered).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            articles = articles.Where(a =>
                a.ArticleCode.ToLower().Contains(q) ||
                a.CompanyName.ToLower().Contains(q)).ToList();
        }

        if (DateTime.TryParse(from, out var fromDate))
            articles = articles.Where(a => a.DeliveredAt.HasValue && a.DeliveredAt.Value.Date >= fromDate.Date).ToList();
        if (DateTime.TryParse(to, out var toDate))
            articles = articles.Where(a => a.DeliveredAt.HasValue && a.DeliveredAt.Value.Date <= toDate.Date).ToList();

        var rows = articles.Select(a => new
        {
            a.Id,
            a.ArticleCode,
            a.CompanyName,
            a.Color,
            Fabrics = a.Fabrics,
            a.Quantity,
            a.DoneDepartments,
            a.TotalDepartments,
            a.DeliveryDate,
            DeliveredAt = a.DeliveredAt
        }).OrderByDescending(r => r.DeliveredAt).ToList();

        return new JsonResult(rows);
    }

    // Undo delivery: un-deliver + revert last Done department to Pending.
    public async Task<IActionResult> OnPostUndoDeliverAsync(int id)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : 0;
        try
        {
            await _workflowService.UndoDeliverAsync(id, userId);
            TempData["SuccessMessage"] = "Delivery undone. The article is back in progress.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToPage("/Admin/History");
    }
}
