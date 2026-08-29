using Application.DTOs.User;
using Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationWeb.Pages.Manager;

[Authorize(Roles = "Manager")]
public class ImpersonateModel : PageModel
{
    private readonly IUserService _userService;

    public ImpersonateModel(IUserService userService)
    {
        _userService = userService;
    }

    // Switch to another user while impersonating (keeps original admin so we can always return).
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var target = (await _userService.GetAllUsersAsync()).FirstOrDefault(u => u.Id == id);
        if (target == null || !target.IsActive)
            return RedirectToPage("/Manager/Dashboard");

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

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return target.Role switch
        {
            "Manager" => RedirectToPage("/Manager/Dashboard"),
            "Reader" => RedirectToPage("/Reader/Dashboard"),
            _ => RedirectToPage("/Admin/Dashboard")
        };
    }

    // Restore the original admin identity.
    public async Task<IActionResult> OnPostBackToAdminAsync()
    {
        var adminId = User.FindFirstValue("ImpersonatedFrom");
        if (string.IsNullOrEmpty(adminId) || !int.TryParse(adminId, out var id))
            return RedirectToPage("/Manager/Dashboard");

        var admin = (await _userService.GetAllUsersAsync()).FirstOrDefault(u => u.Id == id);
        if (admin == null || !admin.IsActive || admin.Role != "Admin")
            return RedirectToPage("/Manager/Dashboard");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.Username),
            new(ClaimTypes.Role, "Admin")
        };
        if (admin.DepartmentId.HasValue)
            claims.Add(new Claim("DepartmentId", admin.DepartmentId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToPage("/Admin/Dashboard");
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Manager/Dashboard");
    }
}
