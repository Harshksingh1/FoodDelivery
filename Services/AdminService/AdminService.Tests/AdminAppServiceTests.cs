using System.Net;
using System.Text.Json;
using AdminService.Application.DTOs;
using AdminService.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AdminService.Tests;

/// <summary>
/// Fake HttpMessageHandler that returns a preset response — lets us test
/// AdminAppService without a real HTTP server.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public FakeHttpMessageHandler(object content, HttpStatusCode status = HttpStatusCode.OK)
    {
        _response = new HttpResponseMessage(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(content), System.Text.Encoding.UTF8, "application/json")
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(_response);
}

[TestFixture]
public class AdminAppServiceTests
{
    private Mock<IHttpClientFactory> _factory;
    private Mock<ILogger<AdminAppService>> _logger;

    [SetUp]
    public void SetUp()
    {
        _factory = new Mock<IHttpClientFactory>();
        _logger = new Mock<ILogger<AdminAppService>>();
    }

    private AdminAppService CreateSut(string clientName, object fakeResponse, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(fakeResponse, status);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        _factory.Setup(f => f.CreateClient(clientName)).Returns(client);

        // Other clients return empty lists by default
        foreach (var name in new[] { "OrderService", "CatalogService", "AuthService", "PaymentService" })
        {
            if (name != clientName)
            {
                var emptyHandler = new FakeHttpMessageHandler(new List<object>());
                var emptyClient = new HttpClient(emptyHandler) { BaseAddress = new Uri("http://localhost") };
                _factory.Setup(f => f.CreateClient(name)).Returns(emptyClient);
            }
        }

        var sut = new AdminAppService(_factory.Object, _logger.Object);
        sut.SetAuthToken("Bearer test-token");
        return sut;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GetDashboard_OrderServiceReturnsOrders_CountsCorrectly()
    {
        var orders = new List<OrderSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Status = "Paid", TotalAmount = 500, GstAmount = 25, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Status = "Delivered", TotalAmount = 300, GstAmount = 15, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Status = "Cancelled", TotalAmount = 200, GstAmount = 10, CreatedAt = DateTime.UtcNow }
        };

        var sut = CreateSut("OrderService", orders);
        var dashboard = await sut.GetDashboardAsync();

        Assert.That(dashboard.TotalOrders, Is.EqualTo(3));
        // Cancelled order should not count in revenue
        Assert.That(dashboard.TotalRevenue, Is.EqualTo(800));
    }

    [Test]
    public async Task GetDashboard_NoOrders_ReturnsZeros()
    {
        var sut = CreateSut("OrderService", new List<OrderSummaryDto>());
        var dashboard = await sut.GetDashboardAsync();

        Assert.That(dashboard.TotalOrders, Is.EqualTo(0));
        Assert.That(dashboard.TotalRevenue, Is.EqualTo(0));
    }

    [Test]
    public async Task GetDashboard_CatalogServiceReturnsRestaurants_CountsActive()
    {
        var restaurants = new List<RestaurantSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Mirchi", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Spice", IsActive = true }
        };

        var sut = CreateSut("CatalogService", restaurants);
        var dashboard = await sut.GetDashboardAsync();

        Assert.That(dashboard.ActiveRestaurants, Is.EqualTo(2));
    }

    // ── Sales Report ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetSalesReport_FiltersOrdersByDateRange()
    {
        var today = DateTime.UtcNow.Date;
        var orders = new List<OrderSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Status = "Delivered", TotalAmount = 500, GstAmount = 25, CreatedAt = today },
            new() { Id = Guid.NewGuid(), Status = "Delivered", TotalAmount = 300, GstAmount = 15, CreatedAt = today.AddDays(-40) } // outside range
        };

        var sut = CreateSut("OrderService", orders);
        var report = await sut.GetSalesReportAsync(today.AddDays(-30), today);

        Assert.That(report.Count, Is.EqualTo(1));
        Assert.That(report[0].Revenue, Is.EqualTo(500));
    }

    [Test]
    public async Task GetSalesReport_ExcludesCancelledOrders()
    {
        var today = DateTime.UtcNow.Date;
        var orders = new List<OrderSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Status = "Delivered", TotalAmount = 500, GstAmount = 25, CreatedAt = today },
            new() { Id = Guid.NewGuid(), Status = "Cancelled", TotalAmount = 200, GstAmount = 10, CreatedAt = today }
        };

        var sut = CreateSut("OrderService", orders);
        var report = await sut.GetSalesReportAsync(today, today);

        Assert.That(report.Count, Is.EqualTo(1));
        Assert.That(report[0].Revenue, Is.EqualTo(500));
    }

    [Test]
    public async Task GetSalesReport_GroupsByDate()
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var orders = new List<OrderSummaryDto>
        {
            new() { Status = "Paid", TotalAmount = 200, GstAmount = 10, CreatedAt = today },
            new() { Status = "Paid", TotalAmount = 300, GstAmount = 15, CreatedAt = today },
            new() { Status = "Paid", TotalAmount = 400, GstAmount = 20, CreatedAt = yesterday }
        };

        var sut = CreateSut("OrderService", orders);
        var report = await sut.GetSalesReportAsync(yesterday, today);

        Assert.That(report.Count, Is.EqualTo(2));
        var todayReport = report.First(r => r.Date == today.ToString("yyyy-MM-dd"));
        Assert.That(todayReport.OrderCount, Is.EqualTo(2));
        Assert.That(todayReport.Revenue, Is.EqualTo(500));
    }

    // ── Partner Report ────────────────────────────────────────────────────────

    [Test]
    public async Task GetPartnerReport_ReturnsAllRestaurants()
    {
        var restaurants = new List<RestaurantSummaryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Mirchi", City = "Delhi", CuisineType = "Indian", Rating = 4.5, IsOpen = true },
            new() { Id = Guid.NewGuid(), Name = "Spice", City = "Mumbai", CuisineType = "Chinese", Rating = 4.0, IsOpen = false }
        };

        var sut = CreateSut("CatalogService", restaurants);
        var report = await sut.GetPartnerReportAsync();

        Assert.That(report.Count, Is.EqualTo(2));
        Assert.That(report[0].RestaurantName, Is.EqualTo("Mirchi"));
        Assert.That(report[1].IsOpen, Is.False);
    }

    [Test]
    public async Task GetPartnerReport_EmptyWhenNoRestaurants()
    {
        var sut = CreateSut("CatalogService", new List<RestaurantSummaryDto>());
        var report = await sut.GetPartnerReportAsync();

        Assert.That(report, Is.Empty);
    }
}
