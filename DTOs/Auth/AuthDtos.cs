using System.ComponentModel.DataAnnotations;

namespace LoginSystem.DTOs.Auth;

public record LoginRequest([property: Required] string Username, [property: Required] string Password);

public record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken,
    Guid UserId, string Username, string Role, Guid? OrganizationId);

public record RefreshRequest([property: Required] string RefreshToken);
public record VerifyEmailRequest([property: Required] Guid UserId, [property: Required] string Token);
public record ForgotPasswordRequest([property: Required, EmailAddress] string Email);
public record ResetPasswordRequest([property: Required] Guid UserId, [property: Required] string Token, [property: Required] string NewPassword);