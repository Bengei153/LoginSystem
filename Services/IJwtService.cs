using LoginSystem.Models;

namespace LoginSystem.Services;

public interface IJwtService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
}