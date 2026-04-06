using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Services;
using OrderService.Domain.Enums;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly OrderAppService _svc;
    public OrderController(OrderAppService svc) => _svc = svc;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string UserRole => User.FindFirstValue(ClaimTypes.Role) ?? "";

    [HttpPost("checkout")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> PlaceOrder([FromBody] CheckoutRequest req)
    {
        var (success, message, data) = await _svc.PlaceOrderAsync(UserId, req);
        return success ? Ok(new { message, data }) : BadRequest(new { message });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var order = await _svc.GetOrderAsync(id);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyOrders()
        => Ok(await _svc.GetCustomerOrdersAsync(UserId));

    [HttpGet("restaurant/{restaurantId:guid}")]
    [Authorize(Roles = "RestaurantPartner")]
    public async Task<IActionResult> RestaurantOrders(Guid restaurantId)
        => Ok(await _svc.GetRestaurantOrdersAsync(restaurantId));

    /// <summary>Restaurant Partner assigns a delivery agent to a ready order</summary>
    [HttpPost("{id:guid}/assign-agent")]
    [Authorize(Roles = "RestaurantPartner,Admin")]
    public async Task<IActionResult> AssignAgent(Guid id, [FromBody] AssignDeliveryAgentRequest req)
    {
        var (success, message) = await _svc.AssignDeliveryAgentAsync(id, req.AgentId, UserRole);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    /// <summary>Delivery Agent sees all orders assigned to them</summary>
    [HttpGet("my-deliveries")]
    [Authorize(Roles = "DeliveryAgent")]
    public async Task<IActionResult> MyDeliveries()
        => Ok(await _svc.GetAgentOrdersAsync(UserId));

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> All([FromQuery] OrderStatus? status)
        => Ok(await _svc.GetAllOrdersAsync(status));

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "RestaurantPartner,DeliveryAgent,Customer,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest req)
    {
        var (success, message) = await _svc.UpdateStatusAsync(id, req.NewStatus, UserRole, req.Note);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }
}
