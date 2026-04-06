namespace CatalogService.Domain.Entities;

public class Restaurant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public string Gst { get; set; } = string.Empty;
    public string Fssai { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public double Rating { get; set; } = 0;
    public int TotalRatings { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsOpen { get; set; } = true;
    public int PrepTimeMinutes { get; set; } = 30;
    public decimal MinOrderAmount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<MenuItem> MenuItems { get; set; } = [];
}
