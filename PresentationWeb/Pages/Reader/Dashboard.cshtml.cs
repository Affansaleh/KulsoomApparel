using Application.DTOs.Article;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationWeb.Pages.Reader;

[Authorize(Roles = "Reader")]
public class DashboardModel : PageModel
{
    private readonly IArticleService _articleService;
    private readonly IUserService _userService;

    public DashboardModel(IArticleService articleService, IUserService userService)
    {
        _articleService = articleService;
        _userService = userService;
    }

    public int ActiveArticles { get; set; }

    public async Task OnGetAsync()
    {
        var articles = await _articleService.GetAllAsync();
        ActiveArticles = articles.Count(a => !a.IsDelivered);

        // When impersonating, expose the user list so the navbar can switch between users.
        if (User.HasClaim(c => c.Type == "ImpersonatedFrom"))
            ViewData["Users"] = await _userService.GetAllUsersAsync();
    }

    // Live table data — same pattern as the Admin dashboard (read-only, no pin/actions).
    public async Task<IActionResult> OnGetArticlesLiveAsync(string? search, string? season, string? status, string? sort)
    {
        var articles = await _articleService.GetAllAsync();
        var today = DateTime.Today;

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
            return new
            {
                a.Id,
                a.CompanyName,
                a.ArticleCode,
                a.Color,
                Fabrics = a.Fabrics,
                AlternateCodes = a.AlternateCodes,
                a.Quantity,
                a.DoneDepartments,
                a.TotalDepartments,
                a.DeliveryDate,
                DaysLeft = daysLeft,
                IsOverdue = daysLeft < 0,
                IsDueSoon = daysLeft >= 0 && daysLeft <= 7,
                a.IsDelivered
            };
        }).ToList();

        if (!string.IsNullOrWhiteSpace(status))
        {
            rows = status switch
            {
                "overdue" => rows.Where(r => r.IsOverdue).ToList(),
                "dueSoon" => rows.Where(r => r.IsDueSoon).ToList(),
                "delivered" => rows.Where(r => r.IsDelivered).ToList(),
                "active" => rows.Where(r => !r.IsDelivered).ToList(),
                _ => rows
            };
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            rows = sort switch
            {
                "low" => rows.OrderBy(r => r.DaysLeft).ToList(),
                "high" => rows.OrderByDescending(r => r.DaysLeft).ToList(),
                _ => rows
            };
        }

        return new JsonResult(rows);
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync();
        return RedirectToPage("/Login");
    }

    // Switch to another user while impersonating (keeps the original admin so we can always return).
    public async Task<IActionResult> OnPostImpersonateAsync(int id)
    {
        var target = (await _userService.GetAllUsersAsync()).FirstOrDefault(u => u.Id == id);
        if (target == null || !target.IsActive)
            return RedirectToPage("/Reader/Dashboard");

        var originalAdmin = User.FindFirstValue("ImpersonatedFrom")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, target.Id.ToString()),
            new(ClaimTypes.Name, target.Username),
            new(ClaimTypes.Role, target.Role)
        };
        if (target.DepartmentId.HasValue)
            claims.Add(new Claim("DepartmentId", target.DepartmentId.Value.ToString()));
        if (!string.IsNullOrEmpty(originalAdmin))
            claims.Add(new Claim("ImpersonatedFrom", originalAdmin));

        var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return target.Role switch
        {
            "Manager" => RedirectToPage("/Manager/Dashboard"),
            "Reader" => RedirectToPage("/Reader/Dashboard"),
            _ => RedirectToPage("/Admin/Dashboard")
        };
    }

    // Restore the original admin identity when this reader is being impersonated.
    public async Task<IActionResult> OnPostBackToAdminAsync()
    {
        var adminId = User.FindFirstValue("ImpersonatedFrom");
        if (string.IsNullOrEmpty(adminId) || !int.TryParse(adminId, out var id))
            return RedirectToPage("/Reader/Dashboard");

        var admin = (await _userService.GetAllUsersAsync()).FirstOrDefault(u => u.Id == id);
        if (admin == null || !admin.IsActive || admin.Role != "Admin")
            return RedirectToPage("/Reader/Dashboard");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.Username),
            new(ClaimTypes.Role, "Admin")
        };

        if (admin.DepartmentId.HasValue)
            claims.Add(new Claim("DepartmentId", admin.DepartmentId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToPage("/Admin/Dashboard");
    }
}
