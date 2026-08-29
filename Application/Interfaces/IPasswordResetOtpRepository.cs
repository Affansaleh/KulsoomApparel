using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces;

public interface IPasswordResetOtpRepository
{
    Task<PasswordResetOtp?> GetValidOtpAsync(int userId, string otpCode);
    Task AddAsync(PasswordResetOtp otp);
    void Update(PasswordResetOtp otp);
    Task SaveChangesAsync();
}