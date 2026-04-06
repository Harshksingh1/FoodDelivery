using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class DeliveryAgentApplicationRequest
{
    [Required] public string Location { get; set; } = string.Empty;
    [Required] public string AadhaarNumber { get; set; } = string.Empty;
    [Required] public string VehicleType { get; set; } = string.Empty;
    [Required] public string VehicleNumber { get; set; } = string.Empty;
    [Required] public string LicenseNumber { get; set; } = string.Empty;
}
