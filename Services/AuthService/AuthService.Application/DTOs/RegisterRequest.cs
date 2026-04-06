using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AuthService.Domain.Enums;

namespace AuthService.Application.DTOs;

public class RegisterRequest
{
    [Required] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, Phone] public string Mobile { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;

    /// <summary>Allowed: Customer, RestaurantPartner, DeliveryAgent</summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RegistrationRole Role { get; set; } = RegistrationRole.Customer;
}
