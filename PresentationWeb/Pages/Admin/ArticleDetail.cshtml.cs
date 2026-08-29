using Application.DTOs.Article;
using Application.DTOs.Workflow;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationWeb.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ArticleDetailModel : PageModel
{
    private readonly IArticleService _articleService;
    private readonly IWorkflowService _workflowService;

    public ArticleDetailModel(IArticleService articleService, IWorkflowService workflowService)
    {
        _articleService = articleService;
        _workflowService = workflowService;
    }

    public ArticleResponseDto? Article { get; set; }
    public List<ArticleDepartmentStatusDto> DepartmentStatuses { get; set; } = new();
    public bool CanUndo { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Article = await _articleService.GetByIdAsync(id);
        if (Article == null)
            return NotFound();

        DepartmentStatuses = await _workflowService.GetByArticleAsync(id);
        // Undo is available whenever a department is InProcess OR Done (one-step staircase).
        CanUndo = DepartmentStatuses.Any(s => s.Status == "InProcess" || s.Status == "Done");

        return Page();
    }

    // Undo last completed department (highest SequenceNumber that is Done)
    public async Task<IActionResult> OnPostUndoLastAsync(int id)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : 0;
        try
        {
            await _workflowService.UndoLastDepartmentAsync(id, userId);
            TempData["SuccessMessage"] = "Last department undone.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToPage("/Admin/ArticleDetail", new { id });
    }
}
