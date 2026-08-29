using Application.DTOs.Article;
using Application.DTOs.Workflow;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationWeb.Pages.Manager;

[Authorize(Roles = "Manager")]
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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Article = await _articleService.GetByIdAsync(id);
        if (Article == null)
            return NotFound();

        DepartmentStatuses = await _workflowService.GetByArticleAsync(id);
        return Page();
    }
}
