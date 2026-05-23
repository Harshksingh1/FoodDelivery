using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;

namespace AuthService.Tests;

[TestFixture]
public class AuthAppServiceTests
{
    private Mock<IUserRepository> _userRepo;
    private Mock<IRefreshTokenRepository> _refreshRepo;
    private Mock<IJwtTokenService> _jwtService;
    private Mock<IEmailService> _emailService;
    private AuthAppService _sut;

    [SetUp]
    public void SetUp()
    {
        _userRepo = new Mock<IUserRepository>();
        _refreshRepo = new Mock<IRefreshTokenRepository>();
        _jwtService = new Mock<IJwtTokenService>();
        _emailService = new Mock<IEmailService>();
        _sut = new AuthAppService(_userRepo.Object, _refreshRepo.Object, _jwtService.Object, _emailService.Object);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Register_DuplicateEmail_ReturnsFalse()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync("test@test.com")).ReturnsAsync(true);

        var req = new RegisterRequest { FullName = "Test", Email = "test@test.com", Mobile = "9999999999", Password = "Test@1234", Role = RegistrationRole.Customer };
        var (success, message) = await _sut.RegisterAsync(req);

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("email already exists"));
    }

    [Test]
    public async Task Register_DuplicateMobile_ReturnsFalse()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByMobileAsync("9999999999")).ReturnsAsync(true);

        var req = new RegisterRequest { FullName = "Test", Email = "new@test.com", Mobile = "9999999999", Password = "Test@1234", Role = RegistrationRole.Customer };
        var (success, message) = await _sut.RegisterAsync(req);

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("mobile number already exists"));
    }

    [Test]
    public async Task Register_ValidRequest_ReturnsSuccess()
    {
        _userRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.ExistsByMobileAsync(It.IsAny<string>())).ReturnsAsync(false);
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userRepo.Setup(r => r.EnsureRoleExistsAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var req = new RegisterRequest { FullName = "Test User", Email = "new@test.com", Mobile = "9876543210", Password = "Test@1234", Role = RegistrationRole.Customer };
        var (success, message) = await _sut.RegisterAsync(req);

        Assert.That(success, Is.True);
        Assert.That(message, Does.Contain("created successfully"));
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Login_InvalidCredentials_ReturnsFalse()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var (success, message, _) = await _sut.LoginAsync(new LoginRequest { Email = "x@x.com", Password = "wrong" });

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("Invalid credentials"));
    }

    [Test]
    public async Task Login_InactiveUser_ReturnsFalse()
    {
        var user = new User { Email = "test@test.com", IsActive = false, Role = UserRole.Customer };
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userRepo.Setup(r => r.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);

        var (success, message, _) = await _sut.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "Test@1234" });

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("deactivated"));
    }

    [Test]
    public async Task Login_AdminUser_SkipsOtp_ReturnsToken()
    {
        var admin = new User { Id = Guid.NewGuid(), Email = "admin@test.com", IsActive = true, Role = UserRole.Admin, FullName = "Admin" };
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(admin);
        _userRepo.Setup(r => r.CheckPasswordAsync(admin, It.IsAny<string>())).ReturnsAsync(true);
        _userRepo.Setup(r => r.GetRolesAsync(admin)).ReturnsAsync(new List<string> { "Admin" });
        _jwtService.Setup(j => j.GenerateAccessToken(admin, It.IsAny<IList<string>>())).Returns("jwt-token");
        _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _refreshRepo.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
        _refreshRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var (success, _, data) = await _sut.LoginAsync(new LoginRequest { Email = "admin@test.com", Password = "Admin@1234" });

        Assert.That(success, Is.True);
        Assert.That(data!.RequiresOtp, Is.False);
        Assert.That(data.AuthData!.AccessToken, Is.EqualTo("jwt-token"));
    }

    [Test]
    public async Task Login_CustomerUser_RequiresOtp()
    {
        var customer = new User { Id = Guid.NewGuid(), Email = "c@test.com", IsActive = true, Role = UserRole.Customer, FullName = "Customer" };
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(customer);
        _userRepo.Setup(r => r.CheckPasswordAsync(customer, It.IsAny<string>())).ReturnsAsync(true);
        _userRepo.Setup(r => r.SetOtpSessionAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _emailService.Setup(e => e.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var (success, _, data) = await _sut.LoginAsync(new LoginRequest { Email = "c@test.com", Password = "Test@1234" });

        Assert.That(success, Is.True);
        Assert.That(data!.RequiresOtp, Is.True);
        Assert.That(data.OtpSessionToken, Is.Not.Empty);
    }

    // ── OTP Verify ────────────────────────────────────────────────────────────

    [Test]
    public async Task VerifyOtp_InvalidToken_ReturnsFalse()
    {
        _userRepo.Setup(r => r.GetByOtpSessionTokenAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var (success, message, _) = await _sut.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionToken = "bad", Otp = "123456" });

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("Invalid or expired session"));
    }

    [Test]
    public async Task VerifyOtp_WrongOtp_ReturnsFalse()
    {
        var user = new User { OtpCode = "111111", OtpExpiry = DateTime.UtcNow.AddMinutes(5) };
        _userRepo.Setup(r => r.GetByOtpSessionTokenAsync(It.IsAny<string>())).ReturnsAsync(user);

        var (success, message, _) = await _sut.VerifyOtpAsync(new VerifyOtpRequest { OtpSessionToken = "tok", Otp = "999999" });

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("Invalid OTP"));
    }

    // ── Change Password ───────────────────────────────────────────────────────

    [Test]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsFalse()
    {
        var user = new User { Id = Guid.NewGuid() };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _userRepo.Setup(r => r.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var (success, message) = await _sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest { CurrentPassword = "wrong", NewPassword = "New@1234" });

        Assert.That(success, Is.False);
        Assert.That(message, Does.Contain("incorrect"));
    }
}
