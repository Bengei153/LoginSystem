using LoginSystem.Data;
using LoginSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LoginSystem.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public RefreshTokenService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(RefreshToken Entity, string RawToken)> IssueAsync(Guid userId, string? ipAddress, string? device)
    {
        var days = double.Parse(_config["Jwt:RefreshTokenDays"]!);
        var raw = SecureTokenGenerator.Generate();

        var entity = new RefreshToken
        {
            UserId = userId,
            Token = SecureTokenGenerator.Hash(raw),
            ExpiresAt = DateTime.UtcNow.AddDays(days),
            IpAddress = ipAddress,
            Device = device
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();
        return (entity, raw);
    }

    public async Task<RefreshToken?> ValidateAsync(string rawToken)
    {
        var hash = SecureTokenGenerator.Hash(rawToken);
        var token = await _db.RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == hash);
        return token is { IsActive: true } ? token : null;
    }

    public async Task RevokeAsync(RefreshToken token)
    {
        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}