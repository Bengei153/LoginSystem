using LoginSystem.Data;
using LoginSystem.DTOs.Auth;
using LoginSystem.Models;
using LoginSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginSystem.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IJwtService _jwt;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IEmailSender _email;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IPasswordHasher<User> hasher, IJwtService jwt,
        IRefreshTokenService refreshTokens, IEmailSender email, IConfiguration config)
    {
        _db = db; _hasher = hasher; _jwt = jwt; _refreshTokens = refreshTokens; _email = email; _config = config;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Username == request.Username.ToLowerInvariant());

        if (user is null)
            return Unauthorized("Invalid username or password.");

        if (LockoutPolicy.IsLockedOut(user))
            return Unauthorized($"Account is locked until {user.LockoutEnd:u}.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            LockoutPolicy.RegisterFailedAttempt(user,
                int.Parse(_config["Lockout:MaxFailedAttempts"]!),
                TimeSpan.FromMinutes(int.Parse(_config["Lockout:LockoutMinutes"]!)));
            await _db.SaveChangesAsync();
            return Unauthorized("Invalid username or password."); // same message either way
        }

        if (!user.EmailConfirmed || user.Status == AccountStatus.Pending)
            return Unauthorized("Please verify your email before logging in.");

        if (user.Status is AccountStatus.Suspended or AccountStatus.Disabled)
            return Unauthorized("This account is no longer active.");

        if (user.OrganizationId is not null && user.Organization is { IsActive: false })
            return Unauthorized("This organization's access has been suspended.");

        LockoutPolicy.RegisterSuccessfulLogin(user);
        await _db.SaveChangesAsync();

        var (accessToken, expires) = _jwt.GenerateAccessToken(user);
        var (_, refreshRaw) = await _refreshTokens.IssueAsync(user.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());

        return Ok(new AuthResponse(accessToken, expires, refreshRaw, user.Id, user.Username, user.Role.ToString(), user.OrganizationId));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
    {
        var stored = await _refreshTokens.ValidateAsync(request.RefreshToken);
        if (stored is null) return Unauthorized("Invalid or expired refresh token.");

        await _refreshTokens.RevokeAsync(stored); // rotate — old token can't be replayed

        var user = stored.User;
        var (accessToken, expires) = _jwt.GenerateAccessToken(user);
        var (_, newRefreshRaw) = await _refreshTokens.IssueAsync(user.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());

        return Ok(new AuthResponse(accessToken, expires, newRefreshRaw, user.Id, user.Username, user.Role.ToString(), user.OrganizationId));
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
    {
        var user = await _db.Users.FindAsync(request.UserId);
        if (user is null) return NotFound();

        var hash = SecureTokenGenerator.Hash(request.Token);
        if (user.EmailVerificationTokenHash != hash || user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
            return BadRequest("Invalid or expired verification token.");

        user.EmailConfirmed = true;
        user.Status = AccountStatus.Active;
        user.EmailVerificationTokenHash = null;
        user.EmailVerificationTokenExpiresAt = null;
        await _db.SaveChangesAsync();

        return Ok("Email verified — you can now log in.");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

        if (user is not null) // always 200 regardless — don't let this endpoint enumerate emails
        {
            var raw = SecureTokenGenerator.Generate();
            user.PasswordResetTokenHash = SecureTokenGenerator.Hash(raw);
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["PasswordReset:TokenExpiryMinutes"]!));
            await _db.SaveChangesAsync();
            await _email.SendAsync(user.Email, "Reset your password", $"UserId: {user.Id}\nToken: {raw}");
        }

        return Ok("If that email is registered, a reset link has been sent.");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _db.Users.FindAsync(request.UserId);
        if (user is null) return BadRequest("Invalid or expired reset token.");

        var hash = SecureTokenGenerator.Hash(request.Token);
        if (user.PasswordResetTokenHash != hash || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            return BadRequest("Invalid or expired reset token.");

        if (!PasswordPolicy.IsValid(request.NewPassword, out var error))
            return BadRequest(error);

        user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
        user.LastPasswordChangeAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok("Password reset — log in with your new password.");
    }
}