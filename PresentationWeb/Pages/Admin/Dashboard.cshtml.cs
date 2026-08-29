using Application.DTOs.Article;
using Application.DTOs.User;
using Application.Enums;
using Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationWeb.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly IArticleService _articleService;
    private readonly IUserService _userService;
    private readonly IAuthService _authService;

    public DashboardModel(IArticleService articleService, IUserService userService, IAuthService authService)
    {
        _articleService = articleService;
        _userService = userService;
        _authService = authService;
    }

    public int ActiveArticles { get; set; }
    public int ManagerCount { get; set; }
    public int ReaderCount { get; set; }

    public async Task OnGetAsync()
    {
        var articles = await _articleService.GetAllAsync();
        ActiveArticles = articles.Count(a => !a.IsDelivered);

        var users = await _userService.GetAllUsersAsync();
        ManagerCount = users.Count(u => u.Role == "Manager" && u.IsActive);
        ReaderCount = users.Count(u => u.Role == "Reader" && u.IsActive);

        ViewData["Users"] = users;
    }

    // Live table data endpoint — called by the client via fetch on search/filter/sort.
    public async Task<IActionResult> OnGetArticlesLiveAsync(string? search, string? season, string? status, string? sort)
    {
        var articles = await _articleService.GetAllAsync();
        var today = DateTime.Today;

        // Part B: hide delivered articles from the active dashboard (always, regardless of filters).
        articles = articles.Where(a => !a.IsDelivered).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            articles = articles.Where(a =>
                a.CompanyName.ToLower().Contains(q) ||
                a.ArticleCode.ToLower().Contains(q) ||
                (a.Color != null && a.Color.ToLower().Contains(q)) ||
                a.AlternateCodes.Any(c => c.ToLower().Contains(q))).ToList();
        }

        if (!string.IsNullOrWhiteSpace(season) && season != "All")
            articles = articles.Where(a => a.Season == season).ToList();

        var rows = articles.Select(a =>
        {
            var daysLeft = (a.DeliveryDate.Date - today).Days;
            return new ArticleRow
            {
                Id = a.Id,
                IsPinned = a.IsPinned,
                IsDelivered = a.IsDelivered,
                CompanyName = a.CompanyName,
                ArticleCode = a.ArticleCode,
                Color = a.Color,
                Fabrics = a.Fabrics,
                AlternateCodes = a.AlternateCodes,
                Quantity = a.Quantity,
                DoneDepartments = a.DoneDepartments,
                TotalDepartments = a.TotalDepartments,
                DeliveryDate = a.DeliveryDate,
                DaysLeft = daysLeft,
                IsOverdue = daysLeft < 0,
                IsDueSoon = daysLeft >= 0 && daysLeft <= 7
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(status))
        {
            rows = status switch
            {
                "overdue" => rows.Where(r => r.IsOverdue).ToList(),
                "dueSoon" => rows.Where(r => r.IsDueSoon).ToList(),
                _ => rows
            };
        }

        // Part A: pinned articles ALWAYS float to the top (primary sort key), then the chosen
        // sort as secondary. "none" (or any unrecognized value) behaves like no secondary sort.
        rows = sort switch
        {
            "low" => rows.OrderByDescending(r => r.IsPinned).ThenBy(r => r.DaysLeft).ToList(),
            "high" => rows.OrderByDescending(r => r.IsPinned).ThenByDescending(r => r.DaysLeft).ToList(),
            _ => rows.OrderByDescending(r => r.IsPinned).ToList()
        };

        return new JsonResult(rows);
    }

    public async Task<IActionResult> OnPostTogglePinAsync(int id)
    {
        try
        {
            await _articleService.TogglePinAsync(id);
            return new JsonResult(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync();
        return RedirectToPage("/Login");
    }

    // Admin-only impersonation: sign in as the selected user, storing the original
    // admin id in a claim so the admin can switch straight back.
    public async Task<IActionResult> OnPostImpersonateAsync(int id)
    {
        var target = (await _userService.GetAllUsersAsync()).FirstOrDefault(u => u.Id == id);
        if (target == null || !target.IsActive)
            return RedirectToPage("/Admin/Dashboard");

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, target.Id.ToString()),
            new(ClaimTypes.Name, target.Username),
            new(ClaimTypes.Role, target.Role)
        };

        if (target.DepartmentId.HasValue)
            claims.Add(new Claim("DepartmentId", target.DepartmentId.Value.ToString()));

        // Remember who initiated the impersonation so we can switch back.
        if (!string.IsNullOrEmpty(adminId))
            claims.Add(new Claim("ImpersonatedFrom", adminId));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // Directly go to the impersonated role's dashboard (no /Index hop).
        return target.Role switch
        {
            "Manager" => RedirectToPage("/Manager/Dashboard"),
            "Reader" => RedirectToPage("/Reader/Dashboard"),
            _ => RedirectToPage("/Admin/Dashboard")
        };
    }

    // Step 1 — send OTP to the logged-in admin's email.
    public async Task<IActionResult> OnPostSendChangeOtpAsync()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return new JsonResult(new { success = false, message = "Session invalid." });

        var result = await _authService.RequestPasswordResetOtpAsync(username);
        return result switch
        {
            PasswordResetRequestResult.OtpSent => new JsonResult(new { success = true }),
            PasswordResetRequestResult.UserNotFound => new JsonResult(new { success = false, message = "User not found." }),
            _ => new JsonResult(new { success = false, message = "Not authorized to reset this password." })
        };
    }

    // Step 2 — verify OTP and apply the new password.
    public async Task<IActionResult> OnPostChangePasswordAsync(string otp, string newPassword)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return new JsonResult(new { success = false, message = "Session invalid." });

        if (string.IsNullOrWhiteSpace(otp))
            return new JsonResult(new { success = false, message = "OTP is required." });

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return new JsonResult(new { success = false, message = "Password must be at least 6 characters." });

        try
        {
            await _authService.VerifyOtpAndResetPasswordAsync(username, otp.Trim(), newPassword);
            return new JsonResult(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }
}

public class ArticleRow
{
    public int Id { get; set; }
    public bool IsPinned { get; set; }
    public bool IsDelivered { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ArticleCode { get; set; } = string.Empty;
    public string? Color { get; set; }
    public List<ArticleFabricDto> Fabrics { get; set; } = new();
    public List<string> AlternateCodes { get; set; } = new();
    public int? Quantity { get; set; }
    public int DoneDepartments { get; set; }
    public int TotalDepartments { get; set; }
    public DateTime DeliveryDate { get; set; }
    public int DaysLeft { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsDueSoon { get; set; }
}
