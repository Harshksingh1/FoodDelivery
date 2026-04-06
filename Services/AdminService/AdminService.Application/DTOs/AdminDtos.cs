namespace AdminService.Application.DTOs;

public class DashboardDto
{
    public int TotalOrders { get; set; }
    public int TodayOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public int ActiveRestaurants { get; set; }
    public int PendingRestaurantApplications { get; set; }
    public int PendingAgentApplications { get; set; }
    public int TotalUsers { get; set; }
}

public class SalesReportDto
{
    public string Date { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal GstCollected { get; set; }
}

public class PartnerReportDto
{
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public double Rating { get; set; }
    public bool IsOpen { get; set; }
}

// Internal DTOs for deserializing downstream responses
public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal GstAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RestaurantSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public double Rating { get; set; }
    public bool IsOpen { get; set; }
    public bool IsActive { get; set; }
}

public class ApplicationSummaryDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
}
