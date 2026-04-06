using AuthService.Domain.Enums;
using AuthService.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUserController : ControllerBase
{
    private readonly AuthDbContext _db;

    public AdminUserController(AuthDbContext db) => _db = db;

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers()
    {
        var users = await _db.Users
            .Where(u => u.Role == UserRole.Customer)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Mobile, u.IsActive, u.CreatedAt })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("delivery-agents")]
    public async Task<IActionResult> GetDeliveryAgents()
    {
        var users = await _db.Users
            .Where(u => u.Role == UserRole.DeliveryAgent)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Mobile, u.IsActive, u.CreatedAt })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("restaurant-partners")]
    public async Task<IActionResult> GetRestaurantPartners()
    {
        var users = await _db.Users
            .Where(u => u.Role == UserRole.RestaurantPartner)
            .Select(u => new { u.Id, u.FullName, u.Email, u.Mobile, u.IsActive, u.CreatedAt })
            .ToListAsync();
        return Ok(users);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });
        if (user.Role == UserRole.Admin) return BadRequest(new { message = "Cannot delete admin account." });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = $"User {user.Email} deleted." });
    }

    [HttpPatch("{userId:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound(new { message = "User not found." });
        if (user.Role == UserRole.Admin) return BadRequest(new { message = "Cannot deactivate admin account." });

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"User {(user.IsActive ? "activated" : "deactivated")}.", isActive = user.IsActive });
    }
}
