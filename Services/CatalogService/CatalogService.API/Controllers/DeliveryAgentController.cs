using System.Security.Claims;
using CatalogService.Application.DTOs;
using CatalogService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.API.Controllers;

/// <summary>Delivery Agent — submit and track onboarding application</summary>
[ApiController]
[Route("api/catalog/delivery-agent")]
[Authorize(Roles = "DeliveryAgent")]
public class DeliveryAgentController : ControllerBase
{
    private readonly CatalogAppService _svc;
    public DeliveryAgentController(CatalogAppService svc) => _svc = svc;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    private string UserEmail => User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

    [HttpPost("applications")]
    public async Task<IActionResult> Apply([FromBody] DeliveryAgentApplicationRequest req)
    {
        var (success, message) = await _svc.ApplyForDeliveryAgentAsync(UserId, UserName, UserEmail, req);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpGet("applications/mine")]
    public async Task<IActionResult> MyApplication()
    {
        var (success, message, data) = await _svc.GetMyAgentApplicationAsync(UserId);
        return success ? Ok(new { message, data }) : NotFound(new { message });
    }
}
