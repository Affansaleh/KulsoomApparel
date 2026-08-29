using Application.DTOs.User;
using Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationWeb.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ImpersonateModel : PageModel
{
    private readonly IUserService _userService;

    public ImpersonateModel(IUserService userService)
    {
        _userService = userService;
    }

    // Admin impersonation handler — callable from any admin page via POST.
    public async Task<IActionResult> OnPostAsync(int id)
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

        if (!string.IsNullOrEmpty(adminId))
            claims.Add(new Claim("ImpersonatedFrom", adminId));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return target.Role switch
        {
            "Manager" => RedirectToPage("/Manager/Dashboard"),
            "Reader" => RedirectToPage("/Reader/Dashboard"),
            _ => RedirectToPage("/Admin/Dashboard")
        };
    }

    public IActionResult OnGet()
    {
        // Direct GET to this page is not meaningful — bounce to dashboard.
        return RedirectToPage("/Admin/Dashboard");
    }
}
