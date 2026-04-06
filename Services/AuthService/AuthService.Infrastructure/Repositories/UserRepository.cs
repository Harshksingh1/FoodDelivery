using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UserRepository(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public Task<User?> GetByEmailAsync(string email) => _userManager.FindByEmailAsync(email);
    public Task<User?> GetByIdAsync(Guid id) => _userManager.FindByIdAsync(id.ToString());

    public Task<User?> GetByOtpSessionTokenAsync(string token) =>
        _userManager.Users.FirstOrDefaultAsync(u => u.OtpSessionToken == token);

    public Task<User?> GetByPasswordResetTokenAsync(string token) =>
        _userManager.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

    public Task<bool> ExistsByEmailAsync(string email) =>
        _userManager.Users.AnyAsync(u => u.Email == email);

    public Task<bool> ExistsByMobileAsync(string mobile) =>
        _userManager.Users.AnyAsync(u => u.Mobile == mobile);

    public Task<IdentityResult> CreateAsync(User user, string password) =>
        _userManager.CreateAsync(user, password);

    public Task<IdentityResult> UpdateAsync(User user) => _userManager.UpdateAsync(user);

    public Task<bool> CheckPasswordAsync(User user, string password) =>
        _userManager.CheckPasswordAsync(user, password);

    public Task<IList<string>> GetRolesAsync(User user) => _userManager.GetRolesAsync(user);

    public async Task EnsureRoleExistsAsync(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }

    public Task<IdentityResult> AddToRoleAsync(User user, string role) =>
        _userManager.AddToRoleAsync(user, role);

    public async Task<IdentityResult> RemoveAndSetPasswordAsync(User user, string newPassword)
    {
        var remove = await _userManager.RemovePasswordAsync(user);
        if (!remove.Succeeded) return remove;
        return await _userManager.AddPasswordAsync(user, newPassword);
    }

    public async Task SetOtpSessionAsync(User user, string sessionToken, string otp)
    {
        user.OtpSessionToken = sessionToken;
        user.OtpCode = otp;
        user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    public async Task ClearOtpSessionAsync(User user)
    {
        user.OtpSessionToken = null;
        user.OtpCode = null;
        user.OtpExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
    }
}
