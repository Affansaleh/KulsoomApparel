using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PresentationWeb.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(roleClaim))
            return RedirectToPage("/Login");

        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
            return RedirectToPage("/Login");

        return role switch
        {
            UserRole.Admin => RedirectToPage("/Admin/Dashboard"),
            UserRole.Manager => RedirectToPage("/Manager/Dashboard"),
            UserRole.Reader => RedirectToPage("/Reader/Dashboard"),
            _ => RedirectToPage("/Login")
        };
    }
}