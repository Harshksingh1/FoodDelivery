using System.Net.Http.Json;
using System.Text.Json;
using AdminService.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace AdminService.Application.Services;

public class AdminAppService
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<AdminAppService> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    // Token is set per-request by the API layer via SetAuthToken
    private string? _authToken;

    public AdminAppService(IHttpClientFactory factory, ILogger<AdminAppService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public void SetAuthToken(string? token) => _authToken = token;

    private HttpClient CreateClient(string name)
    {
        var client = _factory.CreateClient(name);
        if (!string.IsNullOrEmpty(_authToken))
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _authToken);
        }
        return client;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var dashboard = new DashboardDto();

        try
        {
            var orders = await CreateClient("OrderService")
                .GetFromJsonAsync<List<OrderSummaryDto>>("/api/orders", _json) ?? [];
            dashboard.TotalOrders = orders.Count;
            dashboard.TotalRevenue = orders
                .Where(o => o.Status == "Delivered")
                .Sum(o => o.TotalAmount);
            var today = DateTime.UtcNow.Date;
            var todayOrders = orders.Where(o => o.CreatedAt.Date == today).ToList();
            dashboard.TodayOrders = todayOrders.Count;
            dashboard.TodayRevenue = todayOrders
                .Where(o => o.Status == "Delivered")
                .Sum(o => o.TotalAmount);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not fetch orders"); }

        try
        {
            var restaurants = await CreateClient("CatalogService")
                .GetFromJsonAsync<List<RestaurantSummaryDto>>("/api/catalog/restaurants", _json) ?? [];
            // GetAllAsync already filters IsActive=true, so count = active restaurants
            dashboard.ActiveRestaurants = restaurants.Count;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not fetch restaurants"); }

        try
        {
            // Use the public pending count endpoint — no auth needed
            var restaurantApps = await CreateClient("CatalogService")
                .GetFromJsonAsync<List<ApplicationSummaryDto>>("/api/catalog/admin/approvals/restaurants", _json) ?? [];
            dashboard.PendingRestaurantApplications = restaurantApps.Count(a => a.Status == "Pending");
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not fetch restaurant applications"); }

        try
        {
            var agentApps = await CreateClient("CatalogService")
                .GetFromJsonAsync<List<ApplicationSummaryDto>>("/api/catalog/admin/approvals/delivery-agents", _json) ?? [];
            dashboard.PendingAgentApplications = agentApps.Count(a => a.Status == "Pending");
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not fetch agent applications"); }

        try
        {
            var customers = await CreateClient("AuthService")
                .GetFromJsonAsync<List<object>>("/api/auth/admin/users/customers", _json) ?? [];
            var agents = await CreateClient("AuthService")
                .GetFromJsonAsync<List<object>>("/api/auth/admin/users/delivery-agents", _json) ?? [];
            var partners = await CreateClient("AuthService")
                .GetFromJsonAsync<List<object>>("/api/auth/admin/users/restaurant-partners", _json) ?? [];
            dashboard.TotalUsers = customers.Count + agents.Count + partners.Count;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not fetch users"); }

        return dashboard;
    }

    public async Task<List<SalesReportDto>> GetSalesReportAsync(DateTime from, DateTime to)
    {
        try
        {
            var orders = await CreateClient("OrderService")
                .GetFromJsonAsync<List<OrderSummaryDto>>("/api/orders", _json) ?? [];

            return orders
                .Where(o => o.CreatedAt.Date >= from.Date && o.CreatedAt.Date <= to.Date
                         && o.Status == "Delivered")
                .GroupBy(o => o.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new SalesReportDto
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount),
                    GstCollected = g.Sum(o => o.GstAmount)
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch sales report");
            return [];
        }
    }

    public async Task<List<RestaurantRevenueDto>> GetRestaurantRevenueReportAsync(DateTime from, DateTime to)
    {
        try
        {
            var orders = await CreateClient("OrderService")
                .GetFromJsonAsync<List<OrderSummaryDto>>("/api/orders", _json) ?? [];

            return orders
                .Where(o => o.Status == "Delivered" &&
                            o.CreatedAt.Date >= from.Date &&
                            o.CreatedAt.Date <= to.Date)
                .GroupBy(o => new { o.RestaurantId, o.RestaurantName })
                .Select(g => new RestaurantRevenueDto
                {
                    RestaurantId = g.Key.RestaurantId,
                    RestaurantName = g.Key.RestaurantName,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    DailyBreakdown = g
                        .GroupBy(o => o.CreatedAt.Date)
                        .OrderBy(d => d.Key)
                        .Select(d => new RestaurantRevenueDayDto
                        {
                            Date = d.Key.ToString("yyyy-MM-dd"),
                            OrderCount = d.Count(),
                            Revenue = d.Sum(o => o.TotalAmount)
                        }).ToList()
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch restaurant revenue report");
            return [];
        }
    }

    public async Task<List<PartnerReportDto>> GetPartnerReportAsync()
    {
        try
        {
            var restaurants = await CreateClient("CatalogService")
                .GetFromJsonAsync<List<RestaurantSummaryDto>>("/api/catalog/restaurants", _json) ?? [];

            return restaurants.Select(r => new PartnerReportDto
            {
                RestaurantId = r.Id,
                RestaurantName = r.Name,
                City = r.City,
                CuisineType = r.CuisineType,
                Rating = r.Rating,
                IsOpen = r.IsOpen
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch partner report");
            return [];
        }
    }
}
