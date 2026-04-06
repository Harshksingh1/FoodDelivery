using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    public RefreshTokenRepository(AuthDbContext context) => _context = context;

    public Task<RefreshToken?> GetByTokenAsync(string token) =>
        _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token && !r.IsRevoked);

    public async Task AddAsync(RefreshToken token) => await _context.RefreshTokens.AddAsync(token);

    public Task RevokeAsync(RefreshToken token)
    {
        token.IsRevoked = true;
        _context.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync();
        tokens.ForEach(t => t.IsRevoked = true);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
