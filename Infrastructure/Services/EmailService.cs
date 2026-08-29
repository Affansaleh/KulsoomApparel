using Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendOtpEmailAsync(string toEmail, string otpCode)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Smtp:SenderName"],
            _configuration["Smtp:SenderEmail"]));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = "Kulsoom Apparel - Password Reset OTP";

        message.Body = new TextPart("plain")
        {
            Text = $"Your OTP code is: {otpCode}\n\nThis code will expire in 10 mins"
        };

        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(
            _configuration["Smtp:Host"],
            int.Parse(_configuration["Smtp:Port"]!),
            SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(
            _configuration["Smtp:Username"],
            _configuration["Smtp:Password"]);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}