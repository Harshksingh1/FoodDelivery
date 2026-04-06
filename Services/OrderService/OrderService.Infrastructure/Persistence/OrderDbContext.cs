using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Infrastructure.Saga;

namespace OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<OrderSagaState> OrderSagaStates => Set<OrderSagaState>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Status).HasConversion<string>();
            e.Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");
            e.Property(o => o.DeliveryFee).HasColumnType("decimal(18,2)");
            e.Property(o => o.GstAmount).HasColumnType("decimal(18,2)");
            e.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            e.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId);
            e.HasMany(o => o.StatusHistory).WithOne(h => h.Order).HasForeignKey(h => h.OrderId);
        });
        b.Entity<OrderItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
            e.Ignore(i => i.TotalPrice);
        });
        b.Entity<OrderStatusHistory>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Status).HasConversion<string>();
        });
        b.Entity<Cart>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.CustomerId).IsUnique();
            e.HasMany(c => c.Items).WithOne(i => i.Cart).HasForeignKey(i => i.CartId);
        });
        b.Entity<CartItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        });

        b.Entity<OrderSagaState>(e =>
        {
            e.HasKey(s => s.CorrelationId);
            e.Property(s => s.CurrentState).HasMaxLength(64);
            e.Property(s => s.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(s => s.PaymentMethod).HasMaxLength(32);
        });
    }
}
