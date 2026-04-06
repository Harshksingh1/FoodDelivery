using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class VerifyOtpRequest
{
    [Required] public string OtpSessionToken { get; set; } = string.Empty;
    [Required, StringLength(6, MinimumLength = 6)] public string Otp { get; set; } = string.Empty;
}
