using LoginSystem.Models;

namespace LoginSystem.Services;

public interface IRefreshTokenService
{
    Task<(RefreshToken Entity, string RawToken)> IssueAsync(Guid userId, string? ipAddress, string? device);
    Task<RefreshToken?> ValidateAsync(string rawToken);
    Task RevokeAsync(RefreshToken token);
}