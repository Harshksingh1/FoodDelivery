using FoodDelivery.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Enums;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Consumers;

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly OrderDbContext _db;

    public PaymentProcessedConsumer(OrderDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var msg = context.Message;
        var newStatus = msg.Success ? OrderStatus.Paid : OrderStatus.PaymentFailed;
        var note = msg.Success ? "Payment confirmed" : msg.FailureReason;

        // Verify order exists
        var orderExists = await _db.Orders.AnyAsync(o => o.Id == msg.OrderId);
        if (!orderExists) return;

        // Update order status + paymentId via direct SQL — no EF tracking conflict
        await _db.Orders
            .Where(o => o.Id == msg.OrderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.PaymentId, msg.PaymentId)
                .SetProperty(o => o.Status, newStatus)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));

        // Insert history via raw SQL to avoid FK tracking issues
        await _db.Database.ExecuteSqlRawAsync(
            "INSERT INTO OrderStatusHistories (Id, OrderId, Status, Note, ChangedBy, ChangedAt) VALUES ({0}, {1}, {2}, {3}, {4}, {5})",
            Guid.NewGuid(), msg.OrderId, newStatus.ToString(), note ?? "", "PaymentService", DateTime.UtcNow);
    }
}
