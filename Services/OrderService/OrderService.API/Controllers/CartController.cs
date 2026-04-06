using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Services;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = "Customer")]
public class CartController : ControllerBase
{
    private readonly OrderAppService _svc;
    public CartController(OrderAppService svc) => _svc = svc;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet] public async Task<IActionResult> Get() => Ok(await _svc.GetCartAsync(UserId));

    [HttpPost("items")]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest req)
        => Ok(await _svc.AddToCartAsync(UserId, req));

    [HttpPut("items/{menuItemId:guid}")]
    public async Task<IActionResult> Update(Guid menuItemId, [FromBody] UpdateCartItemRequest req)
    {
        await _svc.UpdateCartItemAsync(UserId, menuItemId, req.Quantity);
        return Ok(new { message = "Cart updated." });
    }

    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        await _svc.ClearCartAsync(UserId);
        return Ok(new { message = "Cart cleared." });
    }
}
