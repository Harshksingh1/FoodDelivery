using System.ComponentModel.DataAnnotations;
using OrderService.Domain.Enums;

namespace OrderService.Application.DTOs;

public class AddToCartRequest
{
    [Required] public Guid RestaurantId { get; set; }
    [Required] public string RestaurantName { get; set; } = string.Empty;
    [Required] public Guid MenuItemId { get; set; }
    [Required] public string ItemName { get; set; } = string.Empty;
    [Required] public decimal UnitPrice { get; set; }
    [Range(1, 50)] public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    [Range(0, 50)] public int Quantity { get; set; }
}

public class CheckoutRequest
{
    [Required] public string CustomerName { get; set; } = string.Empty;
    [Required] public string CustomerMobile { get; set; } = string.Empty;
    [Required] public string DeliveryAddress { get; set; } = string.Empty;
    public string? DeliveryInstructions { get; set; }
    public string? PromoCode { get; set; }
    [Required] public string PaymentMethod { get; set; } = "COD";
}

public class AssignDeliveryAgentRequest
{
    [Required] public Guid AgentId { get; set; }
}

public class UpdateOrderStatusRequest
{
    [Required] public OrderStatus NewStatus { get; set; }
    public string? Note { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerMobile { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
    public List<StatusHistoryDto> History { get; set; } = [];
}

public class OrderItemDto
{
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class StatusHistoryDto
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}
