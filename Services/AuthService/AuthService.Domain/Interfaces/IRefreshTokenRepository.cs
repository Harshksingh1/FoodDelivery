using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task AddAsync(RefreshToken token);
    Task RevokeAsync(RefreshToken token);
    Task RevokeAllForUserAsync(Guid userId);
    Task SaveChangesAsync();
}
