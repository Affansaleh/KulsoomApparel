using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.AppDbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PasswordResetOtpRepository : IPasswordResetOtpRepository
{
    private readonly ApplicationDbContext _context;

    public PasswordResetOtpRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PasswordResetOtp?> GetValidOtpAsync(int userId, string otpCode)
    {
        return await _context.PasswordResetOtps
            .Where(o => o.UserId == userId
                     && o.OtpCode == otpCode
                     && !o.IsUsed
                     && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(PasswordResetOtp otp)
    {
        await _context.PasswordResetOtps.AddAsync(otp);
    }

    public void Update(PasswordResetOtp otp)
    {
        _context.PasswordResetOtps.Update(otp);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}