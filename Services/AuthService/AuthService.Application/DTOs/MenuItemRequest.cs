using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class MenuItemRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Required, Range(0.01, 100000)] public decimal Price { get; set; }
    public bool IsVeg { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
}
