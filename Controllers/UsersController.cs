// Controllers/UsersController.cs
using LoginSystem.Data;
using LoginSystem.DTOs.Users;
using LoginSystem.Extensions;
using LoginSystem.Models;
using LoginSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoginSystem.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IUsernameGenerator _usernameGenerator;
    private readonly IEmailSender _email;
    private readonly IConfiguration _config;

    public UsersController(AppDbContext db, IPasswordHasher<User> hasher,
        IUsernameGenerator usernameGenerator, IEmailSender email, IConfiguration config)
    {
        _db = db; _hasher = hasher; _usernameGenerator = usernameGenerator; _email = email; _config = config;
    }

    [HttpPost("org-admins")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<UserResponse>> CreateOrgAdmin(CreateOrgAdminRequest request)
    {
        var org = await _db.Organizations.FindAsync(request.OrganizationId);
        if (org is null || !org.IsActive) return BadRequest("Organization not found or inactive.");

        var (user, error) = await CreateUserAsync(request.Email, request.Password, request.PhoneNumber,
            request.Username, UserRole.OrgAdmin, request.OrganizationId);
        return user is null ? BadRequest(error) : Ok(ToResponse(user));
    }

    [HttpPost("students")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<ActionResult<UserResponse>> CreateStudent(CreateStudentRequest request)
    {
        var orgId = User.GetOrganizationId()!.Value; // the caller's own org — see note above

        var (user, error) = await CreateUserAsync(request.Email, request.Password, request.PhoneNumber,
            request.Username, UserRole.Student, orgId);
        return user is null ? BadRequest(error) : Ok(ToResponse(user));
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<ActionResult<List<UserResponse>>> GetAll()
    {
        var query = _db.Users.AsQueryable();

        if (User.IsInRole("OrgAdmin")) // Org Admins only ever see their own students
        {
            var orgId = User.GetOrganizationId()!.Value;
            query = query.Where(u => u.OrganizationId == orgId && u.Role == UserRole.Student);
        }

        return Ok(await query
            .Select(u => new UserResponse(u.Id, u.Username, u.Email, u.Role.ToString(), u.Status.ToString(), u.OrganizationId))
            .ToListAsync());
    }

    [HttpPatch("{id}/disable")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> Disable(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        if (User.IsInRole("OrgAdmin") &&
            (user.OrganizationId != User.GetOrganizationId() || user.Role != UserRole.Student))
            return Forbid(); // Org Admin can only touch their own students

        user.Status = AccountStatus.Disabled;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        return user is null ? NotFound() : Ok(ToResponse(user));
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(User.GetUserId());
        if (user is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber)) user.PhoneNumber = request.PhoneNumber;
        // Email/username excluded deliberately — those affect uniqueness + verification, own flow.
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "OrgAdmin")]
    public async Task<IActionResult> UpdateStudent(Guid id, UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null || user.OrganizationId != User.GetOrganizationId() || user.Role != UserRole.Student)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber)) user.PhoneNumber = request.PhoneNumber;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // FR2/FR3 — admin-triggered reset, distinct from the self-service forgot-password flow.
    // Reuses the same AuthController.ResetPassword confirm step.
    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "SuperAdmin,OrgAdmin")]
    public async Task<IActionResult> AdminResetPassword(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        if (User.IsInRole("OrgAdmin") && (user.OrganizationId != User.GetOrganizationId() || user.Role != UserRole.Student))
            return Forbid();
        if (User.IsInRole("SuperAdmin") && user.Role != UserRole.OrgAdmin)
            return Forbid(); // Super Admin resets Org Admins only, per FR2

        var raw = SecureTokenGenerator.Generate();
        user.PasswordResetTokenHash = SecureTokenGenerator.Hash(raw);
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["PasswordReset:TokenExpiryMinutes"]!));
        await _db.SaveChangesAsync();
        await _email.SendAsync(user.Email, "Your password was reset", $"UserId: {user.Id}\nToken: {raw}");

        return Ok("Reset link sent to the user's email.");
    }

    private async Task<(User? User, string? Error)> CreateUserAsync(string email, string password,
        string phone, string? requestedUsername, UserRole role, Guid organizationId)
    {
        email = email.ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return (null, "Email already in use.");

        if (!PasswordPolicy.IsValid(password, out var pwError))
            return (null, pwError);

        string username;
        if (string.IsNullOrWhiteSpace(requestedUsername))
        {
            username = await _usernameGenerator.GenerateFromEmailAsync(email);
        }
        else
        {
            username = requestedUsername.ToLowerInvariant();
            if (await _db.Users.AnyAsync(u => u.Username == username))
                return (null, "Username already in use.");
        }

        var user = new User { Username = username, Email = email, PhoneNumber = phone, Role = role, OrganizationId = organizationId };
        user.PasswordHash = _hasher.HashPassword(user, password);

        var rawToken = SecureTokenGenerator.Generate();
        user.EmailVerificationTokenHash = SecureTokenGenerator.Hash(rawToken);
        user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(int.Parse(_config["EmailVerification:TokenExpiryHours"]!));

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await _email.SendAsync(user.Email, "Verify your email", $"UserId: {user.Id}\nToken: {rawToken}");
        await _email.SendAsync(user.Email, "Verify your email", $"UserId: {user.Id}\nToken: {rawToken}");

        var verifyUrl = $"{_config["Frontend:VerifyEmailBaseUrl"]}?userId={user.Id}&token={Uri.EscapeDataString(rawToken)}";
/*
        var emailBody = $"""
            <p>Welcome{(string.IsNullOrWhiteSpace(user.Username) ? "" : $", {user.Username}")}!</p>
            <p>Please verify your email address to activate your account:</p>
            <p><a href="{verifyUrl}">Verify my email</a></p>
            <p>Or copy this link into your browser:<br>{verifyUrl}</p>
            <p>This link expires in {_config["EmailVerification:TokenExpiryHours"]} hours.</p>
            """;*/

        var emailBody = $"""
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f1efe8; padding:32px 16px;">
      <tr>
        <td align="center">
          <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background-color:#ffffff; border-radius:12px; overflow:hidden; border:1px solid #e0ded6;">

            <tr>
              <td style="background-color:#e6f1fb; padding:24px; text-align:center;">
                <span style="font-size:18px; font-weight:600; color:#0c447c; font-family:Arial,Helvetica,sans-serif;">QuizWebApp</span>
              </td>
            </tr>

            <tr>
              <td style="padding:28px 24px;">
                <p style="margin:0 0 12px; font-size:16px; font-family:Arial,Helvetica,sans-serif; color:#1a1a1a;">
                  Welcome{(string.IsNullOrWhiteSpace(user.Username) ? "" : $", {user.Username}")}!
                </p>
                <p style="margin:0 0 24px; font-size:14px; line-height:1.6; font-family:Arial,Helvetica,sans-serif; color:#5f5e5a;">
                  Your account has been created. Verify your email address to activate it.
                </p>

                <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 auto 24px;">
                  <tr>
                    <td style="background-color:#185fa5; border-radius:6px;">
                      <a href="{verifyUrl}" style="display:inline-block; padding:12px 32px; font-size:14px; font-weight:600; color:#ffffff; text-decoration:none; font-family:Arial,Helvetica,sans-serif;">
                        Verify my email
                      </a>
                    </td>
                  </tr>
                </table>

                <p style="margin:0 0 4px; font-size:12px; color:#888780; font-family:Arial,Helvetica,sans-serif;">
                  Or copy this link into your browser:
                </p>
                <p style="margin:0 0 20px; font-size:12px; color:#185fa5; word-break:break-all; font-family:Arial,Helvetica,sans-serif;">
                  {verifyUrl}
                </p>

                <p style="margin:0; font-size:12px; color:#888780; font-family:Arial,Helvetica,sans-serif;">
                  This link expires in {_config["EmailVerification:TokenExpiryHours"]} hours.
                </p>
              </td>
            </tr>

            <tr>
              <td style="border-top:1px solid #e0ded6; padding:12px 24px; text-align:center;">
                <p style="margin:0; font-size:11px; color:#b4b2a9; font-family:Arial,Helvetica,sans-serif;">
                  Sent by QuizWebApp &middot; you're receiving this because an admin created an account for you
                </p>
              </td>
            </tr>

          </table>
        </td>
      </tr>
    </table>
    """;

        await _email.SendAsync(user.Email, "Verify your email", emailBody);

        await _email.SendAsync(user.Email, "Verify your email", emailBody);

        return (user, null);
    }

    private static UserResponse ToResponse(User u) =>
        new(u.Id, u.Username, u.Email, u.Role.ToString(), u.Status.ToString(), u.OrganizationId);
}