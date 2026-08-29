using Application.Enums;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace PresentationWeb.Pages;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly IAuthService _authService;

    public ForgotPasswordModel(IAuthService authService)
    {
        _authService = authService;
    }

    [BindProperty, Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "New password is required."), MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty, Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await _authService.RequestPasswordResetOtpAsync(Username);

        switch (result)
        {
            case PasswordResetRequestResult.UserNotFound:
                ErrorMessage = "User not found.";
                return Page();

            case PasswordResetRequestResult.NotAuthorizedRole:
                return RedirectToPage("/ForgotPasswordContactAdmin");

            case PasswordResetRequestResult.OtpSent:
                TempData["ResetUsername"] = Username;
                TempData["ResetNewPassword"] = NewPassword;
                return RedirectToPage("/ForgotPasswordOtp");

            default:
                ErrorMessage = "Something went wrong. Please try again.";
                return Page();
        }
    }
}