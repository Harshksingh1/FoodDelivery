using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class UpdateProfileRequest
{
    [Required]
    public string FullName { get; set; } = string.Empty;
    [Required, Phone]
    [MinLength(10),MaxLength(10)]
    public string Mobile { get; set; } = string.Empty;
}
