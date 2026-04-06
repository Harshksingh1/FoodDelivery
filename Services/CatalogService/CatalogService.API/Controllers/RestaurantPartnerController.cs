using System.Security.Claims;
using CatalogService.Application.DTOs;
using CatalogService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.API.Controllers;

/// <summary>Restaurant Partner — manage their restaurants, menus, and onboarding applications</summary>
[ApiController]
[Route("api/catalog/partner")]
[Authorize(Roles = "RestaurantPartner")]
public class RestaurantPartnerController : ControllerBase
{
    private readonly CatalogAppService _svc;
    public RestaurantPartnerController(CatalogAppService svc) => _svc = svc;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    private string UserEmail => User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

    // ── My Restaurants ────────────────────────────────────────────────────────

    [HttpGet("restaurants")]
    public async Task<IActionResult> MyRestaurants()
        => Ok(await _svc.GetMyRestaurantsAsync(UserId));

    [HttpPut("restaurants/{restaurantId:guid}")]
    public async Task<IActionResult> UpdateRestaurant(Guid restaurantId, [FromBody] UpdateRestaurantRequest req)
    {
        var (success, message) = await _svc.UpdateRestaurantAsync(restaurantId, UserId, req);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPatch("restaurants/{restaurantId:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid restaurantId, [FromQuery] bool isOpen)
    {
        var (success, message) = await _svc.SetStatusAsync(restaurantId, UserId, isOpen);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("restaurants/{restaurantId:guid}/image")]
    public async Task<IActionResult> UploadRestaurantImage(Guid restaurantId, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file provided." });
        var (success, message, imageUrl) = await _svc.UploadRestaurantImageAsync(restaurantId, UserId, file.OpenReadStream(), file.FileName);
        return success ? Ok(new { message, imageUrl }) : BadRequest(new { message });
    }

    // ── Menu Items ────────────────────────────────────────────────────────────

    [HttpPost("restaurants/{restaurantId:guid}/menu")]
    public async Task<IActionResult> AddMenuItem(Guid restaurantId, [FromBody] UpsertMenuItemRequest req)
    {
        var (success, message) = await _svc.AddMenuItemAsync(restaurantId, UserId, req);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPut("menu/{itemId:guid}")]
    public async Task<IActionResult> UpdateMenuItem(Guid itemId, [FromBody] UpsertMenuItemRequest req)
    {
        var (success, message) = await _svc.UpdateMenuItemAsync(itemId, UserId, req);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpDelete("menu/{itemId:guid}")]
    public async Task<IActionResult> DeleteMenuItem(Guid itemId)
    {
        var (success, message) = await _svc.DeleteMenuItemAsync(itemId, UserId);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("menu/{itemId:guid}/image")]
    public async Task<IActionResult> UploadMenuItemImage(Guid itemId, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file provided." });
        var (success, message, imageUrl) = await _svc.UploadMenuItemImageAsync(itemId, UserId, file.OpenReadStream(), file.FileName);
        return success ? Ok(new { message, imageUrl }) : BadRequest(new { message });
    }

    [HttpDelete("restaurants/{restaurantId:guid}")]
    public async Task<IActionResult> DeleteRestaurant(Guid restaurantId)
    {
        var (success, message) = await _svc.DeleteRestaurantAsync(restaurantId, UserId, isAdmin: false);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    // ── Onboarding Application ────────────────────────────────────────────────

    [HttpPost("applications")]
    public async Task<IActionResult> Apply([FromBody] RestaurantApplicationRequest req)
    {
        var (success, message) = await _svc.ApplyForRestaurantAsync(UserId, UserName, UserEmail, req);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpGet("applications/mine")]
    public async Task<IActionResult> MyApplication()
    {
        var (success, message, data) = await _svc.GetMyRestaurantApplicationAsync(UserId);
        return success ? Ok(new { message, data }) : NotFound(new { message });
    }
}
