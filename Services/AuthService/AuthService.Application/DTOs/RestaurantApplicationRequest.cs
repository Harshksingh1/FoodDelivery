using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

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
