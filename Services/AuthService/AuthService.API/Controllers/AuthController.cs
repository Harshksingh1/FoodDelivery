using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{ 
    private readonly IAuthAppService _authService;

    public AuthController(IAuthAppService authService) => _authService = authService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, message) = await _authService.RegisterAsync(request);
        return success ? Ok(new { message }) : Conflict(new { message });
    }

    /// <summary>Step 1 — validates credentials and sends OTP to email</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, message, data) = await _authService.LoginAsync(request);
        return success ? Ok(new { message, data }) : Unauthorized(new { message });
    }

    /// <summary>Step 2 — submit OTP to receive access + refresh tokens</summary>
    [HttpPost("login/verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var (success, message, data) = await _authService.VerifyOtpAsync(request);
        return success ? Ok(new { message, data }) : Unauthorized(new { message });
    }

    [HttpPost("token/refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var (success, message, data) = await _authService.RefreshTokenAsync(request);
        return success ? Ok(new { message, data }) : Unauthorized(new { message });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var (_, message) = await _authService.LogoutAsync(request.RefreshToken);
        return Ok(new { message });
    }

    [HttpPost("password/forgot")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var (_, message) = await _authService.ForgotPasswordAsync(request);
        return Ok(new { message });
    }

    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var (success, message) = await _authService.ResetPasswordAsync(request);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("password/change")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, message) = await _authService.ChangePasswordAsync(userId, request);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, message, data) = await _authService.GetProfileAsync(userId);
        return success ? Ok(new { message, data }) : NotFound(new { message });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, message) = await _authService.UpdateProfileAsync(userId, request);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }
}
