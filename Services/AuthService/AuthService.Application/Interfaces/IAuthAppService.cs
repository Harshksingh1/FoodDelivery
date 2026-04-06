using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IAuthAppService
{
    Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string Message, LoginResponse? Data)> LoginAsync(LoginRequest request);
    Task<(bool Success, string Message, AuthResponse? Data)> VerifyOtpAsync(VerifyOtpRequest request);
    Task<(bool Success, string Message, AuthResponse? Data)> RefreshTokenAsync(RefreshTokenRequest request);
    Task<(bool Success, string Message)> LogoutAsync(string refreshToken);
    Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request);
    Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<(bool Success, string Message, ProfileResponse? Data)> GetProfileAsync(Guid userId);
    Task<(bool Success, string Message)> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<(bool Success, string Message, string? ImageUrl)> UploadProfileImageAsync(Guid userId, Stream imageStream, string fileName);
}
