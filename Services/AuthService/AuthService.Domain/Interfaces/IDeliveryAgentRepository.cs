using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Interfaces;

public interface IDeliveryAgentRepository
{
    Task<DeliveryAgentApplication?> GetApplicationByIdAsync(Guid id);
    Task<DeliveryAgentApplication?> GetApplicationByUserIdAsync(Guid userId);
    Task<bool> HasApprovedApplicationAsync(Guid userId);
    Task<List<DeliveryAgentApplication>> GetAllApplicationsAsync(ApplicationStatus? status = null);
    Task AddApplicationAsync(DeliveryAgentApplication application);
    Task UpdateApplicationAsync(DeliveryAgentApplication application);
    Task SaveChangesAsync();
}
