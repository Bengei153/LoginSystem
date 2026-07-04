using System.ComponentModel.DataAnnotations;

namespace LoginSystem.DTOs.Auth;

public record LoginRequest([Required] string Username, [Required] string Password);

public record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken,
    Guid UserId, string Username, string Role, Guid? OrganizationId);

public record RefreshRequest([Required] string RefreshToken);
public record VerifyEmailRequest([Required] Guid UserId, [Required] string Token);
public record ForgotPasswordRequest([Required, EmailAddress] string Email);
public record ResetPasswordRequest([Required] Guid UserId, [Required] string Token, [Required] string NewPassword);