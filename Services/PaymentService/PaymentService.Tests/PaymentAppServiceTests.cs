using MassTransit;
using Moq;
using NUnit.Framework;
using PaymentService.Application.DTOs;
using PaymentService.Application.Services;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Interfaces;

namespace PaymentService.Tests;

[TestFixture]
public class PaymentAppServiceTests
{
    private Mock<IPaymentRepository> _repo;
    private Mock<IPublishEndpoint> _bus;
    private PaymentAppService _sut;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IPaymentRepository>();
        _bus = new Mock<IPublishEndpoint>();
        _sut = new PaymentAppService(_repo.Object, _bus.Object);
    }

    // ── Simulate ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Simulate_Success_SavesPaymentWithSuccessStatus()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _bus.Setup(b => b.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var req = new SimulatePaymentRequest
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 500,
            Method = "UPI",
            SimulateFailure = false
        };

        var result = await _sut.SimulateAsync(req);

        Assert.That(result.Status, Is.EqualTo("Success"));
        Assert.That(result.TransactionId, Is.Not.Null);
        Assert.That(result.Amount, Is.EqualTo(500));
    }

    [Test]
    public async Task Simulate_Failure_SavesFailedPayment()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _bus.Setup(b => b.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var req = new SimulatePaymentRequest
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 500,
            Method = "Card",
            SimulateFailure = true
        };

        var result = await _sut.SimulateAsync(req);

        Assert.That(result.Status, Is.EqualTo("Failed"));
        Assert.That(result.TransactionId, Is.Null);
    }

    // ── Refund ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Refund_PaymentNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Payment?)null);

        var (success, message) = await _sut.RefundAsync(new RefundRequest { PaymentId = Guid.NewGuid(), Reason = "Customer request" });

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("not found"));
    }

    [Test]
    public async Task Refund_NotSuccessfulPayment_ReturnsFalse()
    {
        var payment = new Payment { Id = Guid.NewGuid(), Status = "Failed" };
        _repo.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);

        var (success, message) = await _sut.RefundAsync(new RefundRequest { PaymentId = payment.Id, Reason = "Test" });

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("Only successful payments"));
    }

    [Test]
    public async Task Refund_SuccessfulPayment_ReturnsSuccess()
    {
        var payment = new Payment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), Amount = 500, Status = "Success" };
        _repo.Setup(r => r.GetByIdAsync(payment.Id)).ReturnsAsync(payment);
        _repo.Setup(r => r.UpdateAsync(payment)).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _bus.Setup(b => b.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var (success, _) = await _sut.RefundAsync(new RefundRequest { PaymentId = payment.Id, Reason = "Customer request" });

        Assert.That(success, Is.True);
        Assert.That(payment.Status, Is.EqualTo("Refunded"));
    }

    // ── Get By Order ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetByOrder_ReturnsNull_WhenNotFound()
    {
        _repo.Setup(r => r.GetByOrderIdAsync(It.IsAny<Guid>())).ReturnsAsync((Payment?)null);

        var result = await _sut.GetByOrderAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }
}
