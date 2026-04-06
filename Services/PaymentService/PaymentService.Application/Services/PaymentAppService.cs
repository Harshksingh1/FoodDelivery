using FoodDelivery.Shared.Events;
using MassTransit;
using PaymentService.Application.DTOs;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Interfaces;

namespace PaymentService.Application.Services;

public class PaymentAppService
{
    private readonly IPaymentRepository _repo;
    private readonly IPublishEndpoint _bus;

    public PaymentAppService(IPaymentRepository repo, IPublishEndpoint bus)
    {
        _repo = repo;
        _bus = bus;
    }

    public async Task<Payment> SimulateAsync(SimulatePaymentRequest req)
    {
        var success = !req.SimulateFailure;
        var payment = new Payment
        {
            OrderId = req.OrderId,
            CustomerId = req.CustomerId,
            Amount = req.Amount,
            Method = req.Method,
            Status = success ? "Success" : "Failed",
            FailureReason = success ? null : "Simulated payment failure",
            TransactionId = success ? $"TXN-{Guid.NewGuid():N}"[..16] : null,
            ProcessedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(payment);
        await _repo.SaveChangesAsync();

        // Publish to saga
        await _bus.Publish(new PaymentProcessedEvent(
            req.OrderId, payment.Id, success,
            payment.FailureReason, DateTime.UtcNow));

        return payment;
    }

    public async Task<(bool Success, string Message)> RefundAsync(RefundRequest req)
    {
        var payment = await _repo.GetByIdAsync(req.PaymentId);
        if (payment == null) return (false, "Payment not found.");
        if (payment.Status != "Success") return (false, "Only successful payments can be refunded.");

        payment.Status = "Refunded";
        payment.ProcessedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(payment);
        await _repo.SaveChangesAsync();

        await _bus.Publish(new RefundInitiatedEvent(
            payment.OrderId, payment.Id, payment.Amount, req.Reason, DateTime.UtcNow));

        return (true, "Refund initiated.");
    }

    public Task<Payment?> GetByOrderAsync(Guid orderId) => _repo.GetByOrderIdAsync(orderId);
    public Task<List<Payment>> GetAllAsync() => _repo.GetAllAsync();
}
