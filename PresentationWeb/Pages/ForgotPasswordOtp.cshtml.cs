using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace PresentationWeb.Pages;

[AllowAnonymous]
public class ForgotPasswordOtpModel : PageModel
{
    private readonly IAuthService _authService;

    public ForgotPasswordOtpModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty, Required(ErrorMessage = "OTP is required."), StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
    public string OtpCode { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        var username = TempData.Peek("ResetUsername") as string;
        if (string.IsNullOrEmpty(username))
            return RedirectToPage("/ForgotPassword");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var username = TempData["ResetUsername"] as string;
        var newPassword = TempData["ResetNewPassword"] as string;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(newPassword))
            return RedirectToPage("/ForgotPassword");

        if (!ModelState.IsValid)
        {
            TempData.Keep("ResetUsername");
            TempData.Keep("ResetNewPassword");
            return Page();
        }

        try
        {
            await _authService.VerifyOtpAndResetPasswordAsync(username, OtpCode, newPassword);
            TempData["LoginSuccessMessage"] = "Password reset successfully. Please login.";
            return RedirectToPage("/Login");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            TempData["ResetUsername"] = username;
            TempData["ResetNewPassword"] = newPassword;
            return Page();
        }
    }
}