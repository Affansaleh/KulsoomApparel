using Application.DTOs.Article;
using Application.DTOs.Workflow;
using WorkflowSizeBreakdownEntryDto =
    Application.DTOs.Workflow.SizeBreakdownEntryDto;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationWeb.Pages.Manager;

[Authorize(Roles = "Manager")]
public class DashboardModel : PageModel
{
    private readonly IWorkflowService _workflowService;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IQualityService _qualityService;
    private readonly IDeliveryService _deliveryService;
    private readonly IUserService _userService;
    private readonly IArticleService _articleService;

    public DashboardModel(
        IWorkflowService workflowService,
        IDepartmentRepository departmentRepository,
        IQualityService qualityService,
        IDeliveryService deliveryService,
        IUserService userService,
        IArticleService articleService)
    {
        _workflowService = workflowService;
        _departmentRepository = departmentRepository;
        _qualityService = qualityService;
        _deliveryService = deliveryService;
        _userService = userService;
        _articleService = articleService;
    }

    public Department? Department { get; set; }

    public List<ArticleDepartmentStatusDto> Statuses { get; set; } = new();

    public List<StitchingTeam> Teams { get; set; } = new();

    // Article details used by the Manager Dashboard:
    // fabrics, size breakdown, A-Grade, B-Grade and loss.
    public Dictionary<int, ArticleResponseDto> ArticleDetails { get; set; } = new();

    public int DepartmentId { get; set; }

    public DepartmentType DepartmentType { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var deptClaim = User.FindFirstValue("DepartmentId");

        if (!int.TryParse(deptClaim, out var deptId))
            return RedirectToPage("/Login");

        DepartmentId = deptId;

        Department = await _departmentRepository.GetByIdAsync(deptId);

        if (Department == null)
            return RedirectToPage("/Login");

        DepartmentType = Department.Type;

        Statuses =
            await _workflowService.GetPendingByDepartmentAsync(deptId);

        Teams = Department.Teams
            .Where(t => t.IsActive)
            .OrderBy(t => t.Id)
            .ToList();

        // Load complete article details for every article shown
        // in this manager's dashboard.
        foreach (var status in Statuses)
        {
            if (ArticleDetails.ContainsKey(status.ArticleId))
                continue;

            var article =
                await _articleService.GetByIdAsync(status.ArticleId);

            if (article != null)
            {
                ArticleDetails[status.ArticleId] = article;
            }
        }

        // When impersonating, expose the user list so the navbar
        // can switch between users.
        if (User.HasClaim(c => c.Type == "ImpersonatedFrom"))
        {
            ViewData["Users"] =
                await _userService.GetAllUsersAsync();
        }

        return Page();
    }

    // Start work on the manager's department status row.
    public async Task<IActionResult> OnPostStartAsync(
        int statusId,
        int? teamId)
    {
        var userId = int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var uid)
            ? uid
            : 0;

        try
        {
            await _workflowService.StartWorkAsync(
                new StartDepartmentWorkDto
                {
                    ArticleDepartmentStatusId = statusId,
                    AssignedTeamId = teamId
                },
                userId);

            TempData["SuccessMessage"] =
                "Work started.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage("/Manager/Dashboard");
    }

    // End work for normal departments.
    public async Task<IActionResult> OnPostEndAsync(
        int statusId,
        int? outputQty,
        string? note,
        string? stitchedBy)
    {
        var userId = int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var uid)
            ? uid
            : 0;

        try
        {
            await _workflowService.EndWorkAsync(
                new EndDepartmentWorkDto
                {
                    ArticleDepartmentStatusId = statusId,
                    OutputQuantity = outputQty,
                    Note = note,
                    StitchedBy = stitchedBy
                },
                userId);

            TempData["SuccessMessage"] =
                "Work completed.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage("/Manager/Dashboard");
    }

    // Quality & Packing end-work:
    // submit size breakdown and B-Grade quantity.
    public async Task<IActionResult> OnPostQualityAsync(
        int articleId,
        string? bGrade,
        string? sizeLabels,
        string? quantities,
        string? note)
    {
        var userId = int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var uid)
            ? uid
            : 0;

        var labels =
            (sizeLabels ?? string.Empty).Split('|');

        var qtyStrings =
            (quantities ?? string.Empty).Split('|');

        // Labels and quantities must match exactly.
        if (labels.Length != qtyStrings.Length ||
            labels.Length == 0 ||
            labels.Any(string.IsNullOrWhiteSpace))
        {
            TempData["ErrorMessage"] =
                "The size table is incomplete or invalid. Nothing was saved.";

            return RedirectToPage("/Manager/Dashboard");
        }

        // Empty B-Grade means zero.
        // Any entered value must contain digits only.
        var parsedBGrade = 0;

        if (!string.IsNullOrWhiteSpace(bGrade))
        {
            var cleanBGrade = bGrade.Trim();

            if (!cleanBGrade.All(char.IsDigit) ||
                !int.TryParse(
                    cleanBGrade,
                    out parsedBGrade))
            {
                TempData["ErrorMessage"] =
                    "B-Grade quantity must be a whole number. Nothing was saved.";

                return RedirectToPage("/Manager/Dashboard");
            }
        }

        var dto = new QualityGradeEntryDto
        {
            ArticleId = articleId,
            BGradeQuantity = parsedBGrade,
            SizeBreakdowns = new List<Application.DTOs.Workflow.SizeBreakdownEntryDto>()
        };

        for (var i = 0; i < labels.Length; i++)
        {
            var label = labels[i].Trim();
            var rawQuantity = qtyStrings[i].Trim();

            // Empty size fields are treated as zero.
            if (string.IsNullOrWhiteSpace(rawQuantity))
            {
                dto.SizeBreakdowns.Add(
                    new Application.DTOs.Workflow.SizeBreakdownEntryDto
                    {
                        SizeLabel = label,
                        OrderIndex = i + 1,
                        Quantity = 0
                    });

                continue;
            }

            // Reject decimal, negative and non-numeric values.
            if (!rawQuantity.All(char.IsDigit) ||
                !int.TryParse(
                    rawQuantity,
                    out var parsedQuantity))
            {
                TempData["ErrorMessage"] =
                    $"Quantity for size '{label}' must be a whole number. Nothing was saved.";

                return RedirectToPage("/Manager/Dashboard");
            }

            dto.SizeBreakdowns.Add(
                new Application.DTOs.Workflow.SizeBreakdownEntryDto
                {
                    SizeLabel = label,
                    OrderIndex = i + 1,
                    Quantity = parsedQuantity
                });
        }

        dto.Note = note;

        try
        {
            // QualityService validates A-Grade + B-Grade
            // before modifying or saving anything.
            await _qualityService.SubmitGradesAsync(
                dto,
                userId);

            TempData["SuccessMessage"] =
                "Quality grades submitted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage("/Manager/Dashboard");
    }

    // Delivery end-work.
    public async Task<IActionResult> OnPostDeliveryAsync(
        int articleId,
        string? packedBy,
        string? checkedBy,
        int? noOfCartons,
        string? note)
    {
        var userId = int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var uid)
            ? uid
            : 0;

        try
        {
            await _deliveryService.ConfirmDeliveryAsync(
                new DeliveryConfirmDto
                {
                    ArticleId = articleId,
                    PackedBy = packedBy,
                    CheckedBy = checkedBy,
                    NoOfCartons = noOfCartons,
                    Note = note
                },
                userId);

            TempData["SuccessMessage"] =
                "Delivery confirmed. Article moved to history.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage("/Manager/Dashboard");
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync();

        return RedirectToPage("/Login");
    }

    // Switch to another user while impersonating.
    public async Task<IActionResult> OnPostImpersonateAsync(int id)
    {
        var target =
            (await _userService.GetAllUsersAsync())
            .FirstOrDefault(u => u.Id == id);

        if (target == null || !target.IsActive)
            return RedirectToPage("/Manager/Dashboard");

        var originalAdmin =
            User.FindFirstValue("ImpersonatedFrom")
            ?? User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                target.Id.ToString()),

            new(
                ClaimTypes.Name,
                target.Username),

            new(
                ClaimTypes.Role,
                target.Role)
        };

        if (target.DepartmentId.HasValue)
        {
            claims.Add(
                new Claim(
                    "DepartmentId",
                    target.DepartmentId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(originalAdmin))
        {
            claims.Add(
                new Claim(
                    "ImpersonatedFrom",
                    originalAdmin));
        }

        var identity = new ClaimsIdentity(
            claims,
            Microsoft.AspNetCore.Authentication
                .Cookies.CookieAuthenticationDefaults
                .AuthenticationScheme);

        await HttpContext.SignInAsync(
            Microsoft.AspNetCore.Authentication
                .Cookies.CookieAuthenticationDefaults
                .AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return target.Role switch
        {
            "Manager" =>
                RedirectToPage("/Manager/Dashboard"),

            "Reader" =>
                RedirectToPage("/Reader/Dashboard"),

            _ =>
                RedirectToPage("/Admin/Dashboard")
        };
    }

    // Restore the original Admin identity.
    public async Task<IActionResult> OnPostBackToAdminAsync()
    {
        var adminId =
            User.FindFirstValue("ImpersonatedFrom");

        if (string.IsNullOrEmpty(adminId) ||
            !int.TryParse(adminId, out var id))
        {
            return RedirectToPage("/Manager/Dashboard");
        }

        var admin =
            (await _userService.GetAllUsersAsync())
            .FirstOrDefault(u => u.Id == id);

        if (admin == null ||
            !admin.IsActive ||
            admin.Role != "Admin")
        {
            return RedirectToPage("/Manager/Dashboard");
        }

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                admin.Id.ToString()),

            new(
                ClaimTypes.Name,
                admin.Username),

            new(
                ClaimTypes.Role,
                "Admin")
        };

        if (admin.DepartmentId.HasValue)
        {
            claims.Add(
                new Claim(
                    "DepartmentId",
                    admin.DepartmentId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(
            claims,
            Microsoft.AspNetCore.Authentication
                .Cookies.CookieAuthenticationDefaults
                .AuthenticationScheme);

        await HttpContext.SignInAsync(
            Microsoft.AspNetCore.Authentication
                .Cookies.CookieAuthenticationDefaults
                .AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToPage("/Admin/Dashboard");
    }
}