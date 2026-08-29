using Application.Enums;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PresentationWeb.Pages.Admin;

[Authorize(Roles = "Admin")]
public class ChangePasswordModel : PageModel
{
    private readonly IAuthService _authService;

    public ChangePasswordModel(IAuthService authService)
    {
        _authService = authService;
    }

    // Step 1 — send OTP to the logged-in admin's email.
    public async Task<IActionResult> OnPostSendOtpAsync()
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

    public IActionResult OnGet()
    {
        return RedirectToPage("/Admin/Dashboard");
    }
}
