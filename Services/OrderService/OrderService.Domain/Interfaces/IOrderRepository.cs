using OrderService.Domain.Entities;
using OrderService.Domain.Enums;

namespace OrderService.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<List<Order>> GetByCustomerAsync(Guid customerId);
    Task<List<Order>> GetByRestaurantAsync(Guid restaurantId);
    Task<List<Order>> GetByAgentAsync(Guid agentId);
    Task<List<Order>> GetAllAsync(OrderStatus? status);
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task UpdateStatusAsync(Guid orderId, OrderStatus newStatus, string changedBy, string? note);
    Task SaveChangesAsync();
}

public interface ICartRepository
{
    Task<Cart?> GetByCustomerAsync(Guid customerId);
    Task AddAsync(Cart cart);
    Task AddCartItemAsync(CartItem item);
    Task UpdateAsync(Cart cart);
    Task DeleteAsync(Cart cart);
    Task SaveChangesAsync();
}
