namespace AuthService.Application.DTOs;

public class LoginResponse
{
    public bool RequiresOtp { get; set; }
    public string OtpSessionToken { get; set; } = string.Empty;
    public AuthResponse? AuthData { get; set; } // only populated for Admin (no OTP)
}
