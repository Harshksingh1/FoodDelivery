using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;
    public OrderRepository(OrderDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(Guid id) =>
        _db.Orders.Include(o => o.Items).Include(o => o.StatusHistory).FirstOrDefaultAsync(o => o.Id == id);

    public Task<List<Order>> GetByCustomerAsync(Guid customerId) =>
        _db.Orders.Include(o => o.Items).Include(o => o.StatusHistory)
            .Where(o => o.CustomerId == customerId).OrderByDescending(o => o.CreatedAt).ToListAsync();

    public Task<List<Order>> GetByRestaurantAsync(Guid restaurantId) =>
        _db.Orders.Include(o => o.Items).Include(o => o.StatusHistory)
            .Where(o => o.RestaurantId == restaurantId).OrderByDescending(o => o.CreatedAt).ToListAsync();

    public Task<List<Order>> GetByAgentAsync(Guid agentId) =>
        _db.Orders.Include(o => o.Items).Include(o => o.StatusHistory)
            .Where(o => o.DeliveryAgentId == agentId).OrderByDescending(o => o.CreatedAt).ToListAsync();

    public Task<List<Order>> GetAllAsync(OrderStatus? status) =>
        status == null
            ? _db.Orders.Include(o => o.Items).Include(o => o.StatusHistory).OrderByDescending(o => o.CreatedAt).ToListAsync()
            : _db.Orders.Include(o => o.Items).Include(o => o.StatusHistory).Where(o => o.Status == status).OrderByDescending(o => o.CreatedAt).ToListAsync();

    public async Task AddAsync(Order order) => await _db.Orders.AddAsync(order);

    public async Task UpdateAsync(Order order)
    {
        if (_db.Entry(order).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            _db.Orders.Update(order);
        return;
    }

    public async Task UpdateStatusAsync(Guid orderId, OrderStatus newStatus, string changedBy, string? note)
    {
        await _db.Orders
            .Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, newStatus)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));

        // Raw SQL avoids EF FK tracking issues after ExecuteUpdateAsync
        await _db.Database.ExecuteSqlRawAsync(
            "INSERT INTO OrderStatusHistories (Id, OrderId, Status, Note, ChangedBy, ChangedAt) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
            Guid.NewGuid(), orderId, newStatus.ToString(), note ?? "", changedBy, DateTime.UtcNow);
    }

    public async Task AssignDeliveryAgentAsync(Guid orderId, Guid agentId)
    {
        await _db.Orders
            .Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.DeliveryAgentId, agentId)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}

public class CartRepository : ICartRepository
{
    private readonly OrderDbContext _db;
    public CartRepository(OrderDbContext db) => _db = db;

    public Task<Cart?> GetByCustomerAsync(Guid customerId) =>
        _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.CustomerId == customerId);

    public async Task AddAsync(Cart cart) => await _db.Carts.AddAsync(cart);

    public async Task AddCartItemAsync(CartItem item) => await _db.CartItems.AddAsync(item);

    public async Task AddCartItemDirectAsync(CartItem item)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "INSERT INTO CartItems (Id, CartId, MenuItemId, Name, UnitPrice, Quantity) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
            item.Id, item.CartId, item.MenuItemId, item.Name, item.UnitPrice, item.Quantity);
    }

    public async Task UpdateCartItemQuantityAsync(Guid itemId, int newQuantity)
    {
        await _db.CartItems
            .Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Quantity, newQuantity));
    }

    public async Task UpdateCartTimestampAsync(Guid cartId)
    {
        await _db.Carts
            .Where(c => c.Id == cartId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UpdatedAt, DateTime.UtcNow));
    }

    public async Task DeleteCartItemAsync(Guid itemId)
    {
        await _db.CartItems.Where(i => i.Id == itemId).ExecuteDeleteAsync();
    }

    public Task UpdateAsync(Cart cart)
    {
        // Only attach if not already tracked — avoids DbUpdateConcurrencyException
        if (_db.Entry(cart).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            _db.Carts.Update(cart);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Cart cart) { _db.Carts.Remove(cart); return Task.CompletedTask; }
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
