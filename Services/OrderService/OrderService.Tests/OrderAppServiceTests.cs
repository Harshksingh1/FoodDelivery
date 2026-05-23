using MassTransit;
using Moq;
using NUnit.Framework;
using OrderService.Application.DTOs;
using OrderService.Application.Services;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Tests;

[TestFixture]
public class OrderAppServiceTests
{
    private Mock<IOrderRepository> _orderRepo;
    private Mock<ICartRepository> _cartRepo;
    private Mock<IPublishEndpoint> _bus;
    private OrderAppService _sut;

    [SetUp]
    public void SetUp()
    {
        _orderRepo = new Mock<IOrderRepository>();
        _cartRepo = new Mock<ICartRepository>();
        _bus = new Mock<IPublishEndpoint>();
        _sut = new OrderAppService(_orderRepo.Object, _cartRepo.Object, _bus.Object);
    }

    // ── Cart ──────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetCart_ReturnsNull_WhenNoCart()
    {
        _cartRepo.Setup(r => r.GetByCustomerAsync(It.IsAny<Guid>())).ReturnsAsync((Cart?)null);

        var result = await _sut.GetCartAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCart_ReturnsCart_WhenExists()
    {
        var customerId = Guid.NewGuid();
        var cart = new Cart { CustomerId = customerId, RestaurantId = Guid.NewGuid() };
        _cartRepo.Setup(r => r.GetByCustomerAsync(customerId)).ReturnsAsync(cart);

        var result = await _sut.GetCartAsync(customerId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CustomerId, Is.EqualTo(customerId));
    }

    // ── Place Order ───────────────────────────────────────────────────────────

    [Test]
    public async Task PlaceOrder_EmptyCart_ReturnsFalse()
    {
        _cartRepo.Setup(r => r.GetByCustomerAsync(It.IsAny<Guid>())).ReturnsAsync((Cart?)null);

        var (success, message, _) = await _sut.PlaceOrderAsync(Guid.NewGuid(),
            new CheckoutRequest { DeliveryAddress = "123 Main St", PaymentMethod = "COD" });

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("empty"));
    }

    [Test]
    public async Task PlaceOrder_ValidCart_CreatesOrderAndReturnsDto()
    {
        var customerId = Guid.NewGuid();
        var cart = new Cart
        {
            CustomerId = customerId,
            RestaurantId = Guid.NewGuid(),
            RestaurantName = "Mirchi",
            Items = new List<CartItem>
            {
                new() { MenuItemId = Guid.NewGuid(), Name = "Pizza", UnitPrice = 299, Quantity = 2 }
            }
        };
        _cartRepo.Setup(r => r.GetByCustomerAsync(customerId)).ReturnsAsync(cart);
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.DeleteAsync(cart)).Returns(Task.CompletedTask);
        _orderRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        // MassTransit IPublishEndpoint uses generic Publish<T> — setup via base interface
        _bus.Setup(b => b.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var (success, _, data) = await _sut.PlaceOrderAsync(customerId,
            new CheckoutRequest { DeliveryAddress = "123 Main St", PaymentMethod = "COD" });

        Assert.That(success, Is.True);
        Assert.That(data, Is.Not.Null);
        // 299*2 = 598 + 5% GST (29.9) + 30 delivery = 657.9
        Assert.That(data!.TotalAmount, Is.GreaterThan(0));
        Assert.That(data.RestaurantName, Is.EqualTo("Mirchi"));
    }

    // ── Status Update ─────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateStatus_OrderNotFound_ReturnsFalse()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Order?)null);

        var (success, message) = await _sut.UpdateStatusAsync(Guid.NewGuid(), OrderStatus.Paid, "Admin");

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("not found"));
    }

    [Test]
    public async Task UpdateStatus_CustomerCannotSetPaid_ReturnsFalse()
    {
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.PaymentPending };
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var (success, message) = await _sut.UpdateStatusAsync(order.Id, OrderStatus.Paid, "Customer");

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("cannot"));
    }

    [Test]
    public async Task UpdateStatus_AdminCanSetAnyStatus_ReturnsSuccess()
    {
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.PaymentPending };
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _orderRepo.Setup(r => r.UpdateStatusAsync(order.Id, OrderStatus.Cancelled, "Admin", It.IsAny<string>())).Returns(Task.CompletedTask);
        _bus.Setup(b => b.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var (success, _) = await _sut.UpdateStatusAsync(order.Id, OrderStatus.Cancelled, "Admin", "Admin override");

        Assert.That(success, Is.True);
    }

    [Test]
    public async Task UpdateStatus_RestaurantPartner_CanAcceptOrder()
    {
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Paid };
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _orderRepo.Setup(r => r.UpdateStatusAsync(order.Id, OrderStatus.RestaurantAccepted, "RestaurantPartner", It.IsAny<string>())).Returns(Task.CompletedTask);

        var (success, _) = await _sut.UpdateStatusAsync(order.Id, OrderStatus.RestaurantAccepted, "RestaurantPartner");

        Assert.That(success, Is.True);
    }

    [Test]
    public async Task UpdateStatus_DeliveryAgent_CanMarkDelivered()
    {
        var agentId = Guid.NewGuid();
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.OutForDelivery, DeliveryAgentId = agentId };
        _orderRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _orderRepo.Setup(r => r.UpdateStatusAsync(order.Id, OrderStatus.Delivered, "DeliveryAgent", It.IsAny<string>())).Returns(Task.CompletedTask);
        _bus.Setup(b => b.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var (success, _) = await _sut.UpdateStatusAsync(order.Id, OrderStatus.Delivered, "DeliveryAgent");

        Assert.That(success, Is.True);
    }
}
