using FoodDelivery.Shared.Events;
using MassTransit;
using OrderService.Application.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class OrderAppService
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICartRepository _cartRepo;
    private readonly IPublishEndpoint _bus;

    public OrderAppService(IOrderRepository orderRepo, ICartRepository cartRepo, IPublishEndpoint bus)
    {
        _orderRepo = orderRepo;
        _cartRepo = cartRepo;
        _bus = bus;
    }

    // ── Cart ─────────────────────────────────────────────────────────────────

    public async Task<Cart> AddToCartAsync(Guid customerId, AddToCartRequest req)
    {
        var cart = await _cartRepo.GetByCustomerAsync(customerId);

        if (cart != null && cart.RestaurantId != req.RestaurantId)
        {
            await _cartRepo.DeleteAsync(cart);
            await _cartRepo.SaveChangesAsync();
            cart = null;
        }

        if (cart == null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                RestaurantId = req.RestaurantId,
                RestaurantName = req.RestaurantName
            };
            // Add item directly to the new cart's collection before first save
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                MenuItemId = req.MenuItemId,
                Name = req.ItemName,
                UnitPrice = req.UnitPrice,
                Quantity = req.Quantity
            });
            await _cartRepo.AddAsync(cart);
            await _cartRepo.SaveChangesAsync();
        }
        else
        {
            var existing = cart.Items.FirstOrDefault(i => i.MenuItemId == req.MenuItemId);
            if (existing != null)
            {
                // Update quantity via direct SQL — no EF tracking conflict
                await _cartRepo.UpdateCartItemQuantityAsync(existing.Id, existing.Quantity + req.Quantity);
            }
            else
            {
                // Insert new item via direct SQL — no EF tracking conflict
                await _cartRepo.AddCartItemDirectAsync(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    MenuItemId = req.MenuItemId,
                    Name = req.ItemName,
                    UnitPrice = req.UnitPrice,
                    Quantity = req.Quantity
                });
            }
            await _cartRepo.UpdateCartTimestampAsync(cart.Id);
        }

        return (await _cartRepo.GetByCustomerAsync(customerId))!;
    }

    public async Task<Cart?> GetCartAsync(Guid customerId) =>
        await _cartRepo.GetByCustomerAsync(customerId);

    public async Task UpdateCartItemAsync(Guid customerId, Guid menuItemId, int quantity)
    {
        var cart = await _cartRepo.GetByCustomerAsync(customerId);
        if (cart == null) return;
        var item = cart.Items.FirstOrDefault(i => i.MenuItemId == menuItemId);
        if (item == null) return;
        if (quantity <= 0)
            await _cartRepo.DeleteCartItemAsync(item.Id);
        else
        {
            await _cartRepo.UpdateCartItemQuantityAsync(item.Id, quantity);
            await _cartRepo.UpdateCartTimestampAsync(cart.Id);
        }
    }

    public async Task ClearCartAsync(Guid customerId)
    {
        var cart = await _cartRepo.GetByCustomerAsync(customerId);
        if (cart != null) { await _cartRepo.DeleteAsync(cart); await _cartRepo.SaveChangesAsync(); }
    }

    // ── Checkout / Place Order ────────────────────────────────────────────────

    public async Task<(bool Success, string Message, OrderDto? Data)> PlaceOrderAsync(Guid customerId, CheckoutRequest req)
    {
        var cart = await _cartRepo.GetByCustomerAsync(customerId);
        if (cart == null || !cart.Items.Any())
            return (false, "Cart is empty.", null);

        var subTotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var gst = Math.Round(subTotal * 0.05m, 2);
        var delivery = 30m;
        var total = subTotal + gst + delivery;

        var order = new Order
        {
            CustomerId = customerId,
            RestaurantId = cart.RestaurantId,
            RestaurantName = cart.RestaurantName,
            CustomerName = req.CustomerName,
            CustomerMobile = req.CustomerMobile,
            Status = OrderStatus.PaymentPending,
            DeliveryAddress = req.DeliveryAddress,
            DeliveryInstructions = req.DeliveryInstructions,
            PromoCode = req.PromoCode,
            SubTotal = subTotal,
            GstAmount = gst,
            DeliveryFee = delivery,
            TotalAmount = total,
            PaymentMethod = req.PaymentMethod,
            EstimatedDeliveryAt = DateTime.UtcNow.AddMinutes(45),
            Items = cart.Items.Select(i => new OrderItem
            {
                MenuItemId = i.MenuItemId, Name = i.Name,
                UnitPrice = i.UnitPrice, Quantity = i.Quantity
            }).ToList()
        };
        order.StatusHistory.Add(new OrderStatusHistory { OrderId = order.Id, Status = OrderStatus.PaymentPending, ChangedBy = "System" });

        await _orderRepo.AddAsync(order);
        await _cartRepo.DeleteAsync(cart);
        await _orderRepo.SaveChangesAsync();

        // Publish saga start event
        await _bus.Publish(new OrderPlacedEvent(order.Id, customerId, order.RestaurantId, total, req.PaymentMethod, DateTime.UtcNow));

        return (true, "Order placed. Awaiting payment.", MapOrder(order));
    }

    // ── Status Updates ────────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> AssignDeliveryAgentAsync(Guid orderId, Guid agentId, string assignedBy)
    {
        var order = await _orderRepo.GetByIdAsync(orderId);
        if (order == null) return (false, "Order not found.");

        if (order.Status != OrderStatus.ReadyForPickup && order.Status != OrderStatus.Paid &&
            order.Status != OrderStatus.RestaurantAccepted && order.Status != OrderStatus.Preparing)
            return (false, $"Cannot assign agent to order in status '{order.Status}'.");

        await _orderRepo.UpdateStatusAsync(orderId, order.Status, assignedBy, $"Delivery agent {agentId} assigned.");
        await _orderRepo.AssignDeliveryAgentAsync(orderId, agentId);

        await _bus.Publish(new FoodDelivery.Shared.Events.DeliveryAssignedEvent(orderId, agentId, DateTime.UtcNow));

        return (true, "Delivery agent assigned.");
    }

    public async Task<List<OrderDto>> GetAgentOrdersAsync(Guid agentId)
    {
        var orders = await _orderRepo.GetByAgentAsync(agentId);
        return orders.Select(MapOrder).ToList();
    }

    public async Task<(bool Success, string Message)> UpdateStatusAsync(Guid orderId, OrderStatus newStatus, string changedBy, string? note = null)
    {
        var order = await _orderRepo.GetByIdAsync(orderId);
        if (order == null) return (false, "Order not found.");

        // Enforce role-based transition rules
        var allowed = changedBy switch
        {
            "RestaurantPartner" => newStatus is OrderStatus.RestaurantAccepted or OrderStatus.RestaurantRejected
                                                or OrderStatus.Preparing or OrderStatus.ReadyForPickup,
            "DeliveryAgent"     => newStatus is OrderStatus.PickedUp or OrderStatus.OutForDelivery or OrderStatus.Delivered,
            "Customer"          => newStatus is OrderStatus.CancelRequested
                                   && order.Status < OrderStatus.Preparing, // cancel window
            "Admin"             => true, // admin can set any status
            _                   => false
        };

        if (!allowed)
            return (false, $"Role '{changedBy}' cannot set status to '{newStatus}'.");

        // Use direct DB update to avoid EF concurrency tracking issues
        await _orderRepo.UpdateStatusAsync(orderId, newStatus, changedBy, note);

        if (newStatus == OrderStatus.Cancelled)
            await _bus.Publish(new OrderCancelledEvent(orderId, note ?? "", changedBy, DateTime.UtcNow));
        else if (newStatus == OrderStatus.Delivered)
            await _bus.Publish(new OrderDeliveredEvent(orderId, Guid.Parse(changedBy == "DeliveryAgent" && order.DeliveryAgentId.HasValue
                ? order.DeliveryAgentId.Value.ToString() : Guid.Empty.ToString()), DateTime.UtcNow));

        return (true, $"Order status updated to {newStatus}.");
    }

    public async Task<OrderDto?> GetOrderAsync(Guid orderId)
    {
        var o = await _orderRepo.GetByIdAsync(orderId);
        return o == null ? null : MapOrder(o);
    }

    public async Task<List<OrderDto>> GetCustomerOrdersAsync(Guid customerId)
    {
        var orders = await _orderRepo.GetByCustomerAsync(customerId);
        return orders.Select(MapOrder).ToList();
    }

    public async Task<List<OrderDto>> GetRestaurantOrdersAsync(Guid restaurantId)
    {
        var orders = await _orderRepo.GetByRestaurantAsync(restaurantId);
        return orders.Select(MapOrder).ToList();
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync(OrderStatus? status)
    {
        var orders = await _orderRepo.GetAllAsync(status);
        return orders.Select(MapOrder).ToList();
    }

    private static OrderDto MapOrder(Order o) => new()
    {
        Id = o.Id, CustomerId = o.CustomerId, RestaurantId = o.RestaurantId,
        RestaurantName = o.RestaurantName, CustomerName = o.CustomerName,
        CustomerMobile = o.CustomerMobile, Status = o.Status.ToString(),
        DeliveryAddress = o.DeliveryAddress, SubTotal = o.SubTotal,
        DiscountAmount = o.DiscountAmount, DeliveryFee = o.DeliveryFee,
        GstAmount = o.GstAmount, TotalAmount = o.TotalAmount,
        PaymentMethod = o.PaymentMethod, CreatedAt = o.CreatedAt,
        EstimatedDeliveryAt = o.EstimatedDeliveryAt,
        Items = o.Items.Select(i => new OrderItemDto
        {
            MenuItemId = i.MenuItemId, Name = i.Name,
            UnitPrice = i.UnitPrice, Quantity = i.Quantity, TotalPrice = i.TotalPrice
        }).ToList(),
        History = o.StatusHistory.OrderBy(h => h.ChangedAt).Select(h => new StatusHistoryDto
        {
            Status = h.Status.ToString(), Note = h.Note,
            ChangedAt = h.ChangedAt, ChangedBy = h.ChangedBy
        }).ToList()
    };
}
