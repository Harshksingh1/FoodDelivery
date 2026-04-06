using System.Net;
using System.Net.Mail;
using AuthService.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AuthService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config) => _config = config;

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var smtp = _config.GetSection("Email");
        var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"]!))
        {
            Credentials = new NetworkCredential(smtp["Username"], smtp["Password"]),
            EnableSsl = true
        };
        var mail = new MailMessage
        {
            From = new MailAddress(smtp["From"]!, smtp["DisplayName"] ?? "FoodDelivery"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        mail.To.Add(toEmail);
        await client.SendMailAsync(mail);
    }

    public Task SendOtpAsync(string toEmail, string fullName, string otp)
    {
        var body = $"""
            <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:32px;border:1px solid #eee;border-radius:8px;">
                <h2 style="color:#FF6B35;">Your Login OTP</h2>
                <p>Hi {fullName},</p>
                <p>Use the code below to complete your login. It expires in <strong>10 minutes</strong>.</p>
                <div style="text-align:center;margin:32px 0;">
                    <span style="font-size:40px;font-weight:bold;letter-spacing:12px;color:#1a1a1a;background:#f5f5f5;padding:16px 24px;border-radius:8px;">
                        {otp}
                    </span>
                </div>
                <p style="color:#888;font-size:13px;">If you didn't attempt to log in, please secure your account immediately.</p>
            </div>
            """;
        return SendAsync(toEmail, "Your FoodDelivery Login OTP", body);
    }

    public Task SendPasswordResetAsync(string toEmail, string fullName, string token)
    {
        var baseUrl = _config["App:FrontendUrl"] ?? "http://localhost:4200";
        var link = $"{baseUrl}/auth/reset-password?token={token}";
        var body = $"""
            <h2>Password Reset Request</h2>
            <p>Hi {fullName}, we received a request to reset your password.</p>
            <a href="{link}" style="background:#FF6B35;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold;">
                Reset Password
            </a>
            <p>This link expires in 1 hour.</p>
            <p>If you didn't request this, please ignore this email.</p>
            """;
        return SendAsync(toEmail, "Reset your FoodDelivery password", body);
    }
}
