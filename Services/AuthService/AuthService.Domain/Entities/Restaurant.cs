namespace AuthService.Domain.Entities;

public class Restaurant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string RestaurantName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string CuisineType { get; set; } = string.Empty;
    public string Gst { get; set; } = string.Empty;
    public string Fssai { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public double Rating { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MenuItem> MenuItems { get; set; } = [];
}
