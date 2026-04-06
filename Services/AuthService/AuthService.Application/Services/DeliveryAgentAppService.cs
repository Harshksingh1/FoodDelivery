using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Services;

public class DeliveryAgentAppService : IDeliveryAgentAppService
{
    private readonly IDeliveryAgentRepository _repo;
    private readonly IUserRepository _userRepo;

    public DeliveryAgentAppService(IDeliveryAgentRepository repo, IUserRepository userRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
    }

    public async Task<(bool Success, string Message)> ApplyAsync(Guid userId, DeliveryAgentApplicationRequest request)
    {
        // Block if already approved — one agent per user
        if (await _repo.HasApprovedApplicationAsync(userId))
            return (false, "You are already an approved delivery agent.");

        var existing = await _repo.GetApplicationByUserIdAsync(userId);
        if (existing != null && existing.Status == ApplicationStatus.Pending)
            return (false, "You already have a pending application.");

        var application = new DeliveryAgentApplication
        {
            UserId = userId,
            Location = request.Location,
            AadhaarNumber = request.AadhaarNumber,
            VehicleType = request.VehicleType,
            VehicleNumber = request.VehicleNumber,
            LicenseNumber = request.LicenseNumber
        };

        await _repo.AddApplicationAsync(application);
        await _repo.SaveChangesAsync();
        return (true, "Application submitted. Awaiting admin approval.");
    }

    public async Task<(bool Success, string Message, DeliveryAgentApplication? Data)> GetMyApplicationAsync(Guid userId)
    {
        var app = await _repo.GetApplicationByUserIdAsync(userId);
        return app == null
            ? (false, "No application found.", null)
            : (true, "Application retrieved.", app);
    }

    public async Task<(bool Success, string Message)> ReviewApplicationAsync(Guid applicationId, ReviewApplicationRequest request)
    {
        var app = await _repo.GetApplicationByIdAsync(applicationId);
        if (app == null) return (false, "Application not found.");

        app.Status = request.Status;
        app.RejectionReason = request.RejectionReason;
        app.ReviewedAt = DateTime.UtcNow;

        if (request.Status == ApplicationStatus.Approved)
        {
            var user = await _userRepo.GetByIdAsync(app.UserId);
            if (user != null)
            {
                await _userRepo.EnsureRoleExistsAsync("DeliveryAgent");
                await _userRepo.AddToRoleAsync(user, "DeliveryAgent");
                user.Role = Domain.Enums.UserRole.DeliveryAgent;
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepo.UpdateAsync(user);
            }
        }

        await _repo.UpdateApplicationAsync(app);
        await _repo.SaveChangesAsync();
        return (true, $"Application {request.Status}.");
    }

    public Task<List<DeliveryAgentApplication>> GetAllApplicationsAsync(ApplicationStatus? status) =>
        _repo.GetAllApplicationsAsync(status);
}
