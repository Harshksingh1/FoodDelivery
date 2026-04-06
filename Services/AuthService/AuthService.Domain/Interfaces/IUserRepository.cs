using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByOtpSessionTokenAsync(string token);
    Task<User?> GetByPasswordResetTokenAsync(string token);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByMobileAsync(string mobile);
    Task<IdentityResult> CreateAsync(User user, string password);
    Task<IdentityResult> UpdateAsync(User user);
    Task<bool> CheckPasswordAsync(User user, string password);
    Task<IList<string>> GetRolesAsync(User user);
    Task EnsureRoleExistsAsync(string role);
    Task<IdentityResult> AddToRoleAsync(User user, string role);
    Task<IdentityResult> RemoveAndSetPasswordAsync(User user, string newPassword);
    Task SetOtpSessionAsync(User user, string sessionToken, string otp);
    Task ClearOtpSessionAsync(User user);
}
