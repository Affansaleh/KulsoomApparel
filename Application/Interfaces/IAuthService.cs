using Application.DTOs.User;
using Application.Enums;
using Domain.Entities;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<PasswordResetRequestResult> RequestPasswordResetOtpAsync(string username);
    Task VerifyOtpAndResetPasswordAsync(string username, string otpCode, string newPassword);
    Task AdminResetUserPasswordAsync(int targetUserId, string newPassword);
    Task<User> ValidateCredentialsAsync(UserLoginDto dto);
}