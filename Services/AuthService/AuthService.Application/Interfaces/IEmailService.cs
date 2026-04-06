namespace AuthService.Application.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string fullName, string token);
    Task SendOtpAsync(string toEmail, string fullName, string otp);
}
