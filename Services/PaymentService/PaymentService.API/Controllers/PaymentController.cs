using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.DTOs;
using PaymentService.Application.Services;

namespace PaymentService.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly PaymentAppService _svc;
    public PaymentController(PaymentAppService svc) => _svc = svc;

    [HttpPost("simulate")]
    public async Task<IActionResult> Simulate([FromBody] SimulatePaymentRequest req)
        => Ok(await _svc.SimulateAsync(req));

    [HttpPost("refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Refund([FromBody] RefundRequest req)
    {
        var (success, message) = await _svc.RefundAsync(req);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrder(Guid orderId)
    {
        var p = await _svc.GetByOrderAsync(orderId);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll() => Ok(await _svc.GetAllAsync());
}
