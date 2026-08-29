using Application.DTOs.User;
using Application.Enums;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IPasswordResetOtpRepository _otpRepository;

    public AuthService(IUserRepository userRepository, IEmailService emailService, IConfiguration configuration , IPasswordResetOtpRepository otpRepository)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration = configuration;
        _otpRepository = otpRepository;
    }

    public async Task<User> ValidateCredentialsAsync(UserLoginDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account is not active.");

        return user;
    }

    public async Task VerifyOtpAndResetPasswordAsync(string username, string otpCode, string newPassword)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        var otp = await _otpRepository.GetValidOtpAsync(user.Id, otpCode);
        if (otp == null)
            throw new InvalidOperationException("Invalid or expired OTP.");

        otp.IsUsed = true;
        _otpRepository.Update(otp);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _userRepository.Update(user);

        await _otpRepository.SaveChangesAsync();
        await _userRepository.SaveChangesAsync();
    }
    public async Task<PasswordResetRequestResult> RequestPasswordResetOtpAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);

        if (user == null)
            return PasswordResetRequestResult.UserNotFound;

        if (user.Role != Domain.Enums.UserRole.Admin)
            return PasswordResetRequestResult.NotAuthorizedRole;

        var otpCode = new Random().Next(100000, 999999).ToString();

        var otp = new PasswordResetOtp
        {
            UserId = user.Id,
            OtpCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        await _otpRepository.AddAsync(otp);
        await _otpRepository.SaveChangesAsync();

        var destinationEmail = _configuration["Smtp:SenderEmail"]!;
        await _emailService.SendOtpEmailAsync(destinationEmail, otpCode);

        return PasswordResetRequestResult.OtpSent;
    }

    public async Task AdminResetUserPasswordAsync(int targetUserId, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();
    }
}