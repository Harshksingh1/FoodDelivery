using AuthService.Application.DTOs;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Application.Interfaces;

public interface IDeliveryAgentAppService
{
    Task<(bool Success, string Message)> ApplyAsync(Guid userId, DeliveryAgentApplicationRequest request);
    Task<(bool Success, string Message, DeliveryAgentApplication? Data)> GetMyApplicationAsync(Guid userId);
    Task<(bool Success, string Message)> ReviewApplicationAsync(Guid applicationId, ReviewApplicationRequest request);
    Task<List<DeliveryAgentApplication>> GetAllApplicationsAsync(ApplicationStatus? status);
}
