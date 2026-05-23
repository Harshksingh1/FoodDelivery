using CatalogService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.API.Controllers;

/// <summary>Public read-only catalog endpoints — no auth required</summary>
[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly CatalogAppService _svc;
    public CatalogController(CatalogAppService svc) => _svc = svc;

    [HttpGet("restaurants")]
    public async Task<IActionResult> GetRestaurants(
        [FromQuery] string? city, [FromQuery] string? cuisine, [FromQuery] bool? isOpen)
        => Ok(await _svc.GetRestaurantsAsync(city, cuisine, isOpen));

    [HttpGet("restaurants/search")]
    public async Task<IActionResult> SearchRestaurants([FromQuery] string q)
        => Ok(await _svc.SearchRestaurantsAsync(q));

    [HttpGet("menu/search")]
    public async Task<IActionResult> SearchMenu([FromQuery] string q)
        => Ok(await _svc.SearchMenuAsync(q));

    [HttpGet("restaurants/{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var (data, menu) = await _svc.GetRestaurantDetailAsync(id);
        return data == null ? NotFound() : Ok(new { restaurant = data, menu });
    }

    [HttpGet("restaurants/{restaurantId:guid}/menu")]
    public async Task<IActionResult> GetMenu(Guid restaurantId)
        => Ok(await _svc.GetMenuAsync(restaurantId));

    [HttpPost("restaurants/{restaurantId:guid}/rate")]
    [Authorize]
    public async Task<IActionResult> RateRestaurant(Guid restaurantId, [FromBody] RateRequest req)
    {
        var (success, message) = await _svc.RateRestaurantAsync(restaurantId, req.Stars);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }
}

public record RateRequest(int Stars);
