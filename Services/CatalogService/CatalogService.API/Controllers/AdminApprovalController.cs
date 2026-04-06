using System.Security.Claims;
using CatalogService.Application.DTOs;
using CatalogService.Application.Services;
using CatalogService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.API.Controllers;

/// <summary>Admin approval of Restaurant Partner and Delivery Agent applications</summary>
[ApiController]
[Route("api/catalog/admin/approvals")]
[Authorize(Roles = "Admin")]
public class AdminApprovalController : ControllerBase
{
    private readonly CatalogAppService _svc;
    public AdminApprovalController(CatalogAppService svc) => _svc = svc;

    [HttpDelete("restaurants/{restaurantId:guid}")]
    public async Task<IActionResult> DeleteRestaurant(Guid restaurantId)
    {
        var (success, message) = await _svc.DeleteRestaurantAsync(restaurantId, Guid.Empty, isAdmin: true);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    // ── Restaurant Applications ───────────────────────────────────────────────

    [HttpGet("restaurants")]
    public async Task<IActionResult> GetRestaurantApplications([FromQuery] ApplicationStatus? status)
        => Ok(await _svc.GetAllRestaurantApplicationsAsync(status));

    [HttpPost("restaurants/{id:guid}/review")]
    public async Task<IActionResult> ReviewRestaurantApplication(Guid id, [FromBody] ReviewApplicationRequest req)
    {
        var (success, message) = await _svc.ReviewRestaurantApplicationAsync(id, req);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    // ── Delivery Agent Applications ───────────────────────────────────────────

    [HttpGet("delivery-agents")]
    public async Task<IActionResult> GetAgentApplications([FromQuery] ApplicationStatus? status)
        => Ok(await _svc.GetAllAgentApplicationsAsync(status));

    [HttpPost("delivery-agents/{id:guid}/review")]
    public async Task<IActionResult> ReviewAgentApplication(Guid id, [FromBody] ReviewApplicationRequest req)
    {
        var (success, message) = await _svc.ReviewAgentApplicationAsync(id, req);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }
}
