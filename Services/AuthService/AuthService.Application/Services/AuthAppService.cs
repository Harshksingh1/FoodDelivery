using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services;

public class AuthAppService : IAuthAppService
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshRepo;
    private readonly IJwtTokenService _jwtService;
    private readonly IEmailService _emailService;

    public AuthAppService(IUserRepository userRepo, IRefreshTokenRepository refreshRepo,
        IJwtTokenService jwtService, IEmailService emailService)
    {
        _userRepo = userRepo;
        _refreshRepo = refreshRepo;
        _jwtService = jwtService;
        _emailService = emailService;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepo.ExistsByEmailAsync(request.Email.ToLowerInvariant()))
            return (false, "An account with this email already exists.");

        if (await _userRepo.ExistsByMobileAsync(request.Mobile))
            return (false, "An account with this mobile number already exists.");

        var roleName = request.Role.ToString(); // RegistrationRole maps 1:1 to UserRole names

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email.ToLowerInvariant(),
            UserName = request.Email.ToLowerInvariant(),
            Mobile = request.Mobile,
            Role = Enum.Parse<Domain.Enums.UserRole>(request.Role.ToString()),
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userRepo.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        await _userRepo.EnsureRoleExistsAsync(roleName);
        await _userRepo.AddToRoleAsync(user, roleName);

        return (true, "Account created successfully. You can now log in.");
    }

    public async Task<(bool Success, string Message, LoginResponse? Data)> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email.ToLowerInvariant());

        if (user == null || !await _userRepo.CheckPasswordAsync(user, request.Password))
            return (false, "Invalid credentials.", null);

        if (!user.IsActive)
            return (false, "Your account has been deactivated. Please contact support.", null);

        // Admin bypasses OTP — issue JWT directly
        if (user.Role == Domain.Enums.UserRole.Admin)
        {
            var roles = await _userRepo.GetRolesAsync(user);
            var accessToken = _jwtService.GenerateAccessToken(user, roles);
            var refreshTokenStr = _jwtService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Token = refreshTokenStr,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
            await _refreshRepo.AddAsync(refreshToken);
            await _refreshRepo.SaveChangesAsync();

            return (true, "Login successful.", new LoginResponse
            {
                RequiresOtp = false,
                OtpSessionToken = string.Empty,
                AuthData = new AuthResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshTokenStr,
                    Role = roles.FirstOrDefault() ?? user.Role.ToString(),
                    FullName = user.FullName,
                    Email = user.Email!,
                    UserId = user.Id
                }
            });
        }

        // All other roles — send OTP
        var otp = GenerateOtp();
        var sessionToken = Guid.NewGuid().ToString("N");

        await _userRepo.SetOtpSessionAsync(user, sessionToken, otp);
        await _emailService.SendOtpAsync(user.Email!, user.FullName, otp);

        return (true, "OTP sent to your registered email.", new LoginResponse
        {
            RequiresOtp = true,
            OtpSessionToken = sessionToken
        });
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> VerifyOtpAsync(VerifyOtpRequest request)
    {
        var user = await _userRepo.GetByOtpSessionTokenAsync(request.OtpSessionToken);

        if (user == null)
            return (false, "Invalid or expired session. Please login again.", null);

        if (user.OtpExpiry < DateTime.UtcNow)
        {
            await _userRepo.ClearOtpSessionAsync(user);
            return (false, "OTP has expired. Please login again.", null);
        }

        if (user.OtpCode != request.Otp)
            return (false, "Invalid OTP.", null);

        await _userRepo.ClearOtpSessionAsync(user);

        var roles = await _userRepo.GetRolesAsync(user);
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        var refreshTokenStr = _jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenStr,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        await _refreshRepo.AddAsync(refreshToken);
        await _refreshRepo.SaveChangesAsync();

        return (true, "Login successful.", new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenStr,
            Role = roles.FirstOrDefault() ?? user.Role.ToString(),
            FullName = user.FullName,
            Email = user.Email!,
            UserId = user.Id
        });
    }

    public async Task<(bool Success, string Message, AuthResponse? Data)> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var token = await _refreshRepo.GetByTokenAsync(request.RefreshToken);

        if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            return (false, "Invalid or expired refresh token.", null);

        var user = await _userRepo.GetByIdAsync(token.UserId);
        if (user == null || !user.IsActive)
            return (false, "User not found or inactive.", null);

        // Rotate refresh token
        await _refreshRepo.RevokeAsync(token);

        var roles = await _userRepo.GetRolesAsync(user);
        var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
        var newRefreshTokenStr = _jwtService.GenerateRefreshToken();

        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenStr,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        await _refreshRepo.AddAsync(newRefreshToken);
        await _refreshRepo.SaveChangesAsync();

        return (true, "Token refreshed.", new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenStr,
            Role = roles.FirstOrDefault() ?? user.Role.ToString(),
            FullName = user.FullName,
            Email = user.Email!,
            UserId = user.Id
        });
    }

    public async Task<(bool Success, string Message)> LogoutAsync(string refreshToken)
    {
        var token = await _refreshRepo.GetByTokenAsync(refreshToken);
        if (token == null) return (true, "Logged out.");

        await _refreshRepo.RevokeAllForUserAsync(token.UserId);
        await _refreshRepo.SaveChangesAsync();
        return (true, "Logged out successfully.");
    }

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email.ToLowerInvariant());
        if (user == null) return (true, "If that email exists, a reset link has been sent.");

        var resetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        await _emailService.SendPasswordResetAsync(user.Email!, user.FullName, resetToken);

        return (true, "If that email exists, a reset link has been sent.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userRepo.GetByPasswordResetTokenAsync(request.Token);
        if (user == null) return (false, "Invalid or expired reset token.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            return (false, "Reset token has expired. Please request a new one.");

        await _userRepo.RemoveAndSetPasswordAsync(user, request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        return (true, "Password reset successfully.");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return (false, "User not found.");

        if (!await _userRepo.CheckPasswordAsync(user, request.CurrentPassword))
            return (false, "Current password is incorrect.");

        var result = await _userRepo.RemoveAndSetPasswordAsync(user, request.NewPassword);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        return (true, "Password changed successfully.");
    }

    public async Task<(bool Success, string Message, ProfileResponse? Data)> GetProfileAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return (false, "User not found.", null);

        var roles = await _userRepo.GetRolesAsync(user);
        return (true, "Profile retrieved.", new ProfileResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Mobile = user.Mobile,
            Role = roles.FirstOrDefault() ?? user.Role.ToString(),
            CreatedAt = user.CreatedAt
        });
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return (false, "User not found.");

        user.FullName = request.FullName;
        user.Mobile = request.Mobile;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user);
        return (true, "Profile updated successfully.");
    }

    private static string GenerateOtp() =>
        Random.Shared.Next(100000, 999999).ToString();
}
