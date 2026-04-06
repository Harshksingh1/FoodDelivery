using System.ComponentModel.DataAnnotations;
using CatalogService.Domain.Enums;

namespace CatalogService.Application.DTOs;

// ── Public read DTOs ──────────────────────────────────────────────────────────

public class RestaurantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public double Rating { get; set; }
    public bool IsOpen { get; set; }
    public int PrepTimeMinutes { get; set; }
    public decimal MinOrderAmount { get; set; }
}

public class MenuItemDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsVeg { get; set; }
    public bool IsAvailable { get; set; }
    public double Rating { get; set; }
}

// ── Partner manage restaurant ─────────────────────────────────────────────────

public class UpdateRestaurantRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string Address { get; set; } = string.Empty;
    [Required] public string City { get; set; } = string.Empty;
    [Required] public string Pincode { get; set; } = string.Empty;
    [Required] public string CuisineType { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; } = 30;
    public decimal MinOrderAmount { get; set; } = 0;
}

public class UpsertMenuItemRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    [Required, Range(0.01, 100000)] public decimal Price { get; set; }
    public bool IsVeg { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
}

// ── Applications ──────────────────────────────────────────────────────────────

public class RestaurantApplicationRequest
{
    [Required] public string RestaurantName { get; set; } = string.Empty;
    [Required] public string Address { get; set; } = string.Empty;
    [Required] public string City { get; set; } = string.Empty;
    [Required] public string Pincode { get; set; } = string.Empty;
    [Required] public string CuisineType { get; set; } = "Indian";
    [Required] public string Gst { get; set; } = string.Empty;
    [Required] public string Fssai { get; set; } = string.Empty;
}

public class DeliveryAgentApplicationRequest
{
    [Required] public string Location { get; set; } = string.Empty;
    [Required] public string AadhaarNumber { get; set; } = string.Empty;
    [Required] public string VehicleType { get; set; } = string.Empty;
    [Required] public string VehicleNumber { get; set; } = string.Empty;
    [Required] public string LicenseNumber { get; set; } = string.Empty;
}

public class ReviewApplicationRequest
{
    [Required] public ApplicationStatus Status { get; set; }
    public string? RejectionReason { get; set; }
}
