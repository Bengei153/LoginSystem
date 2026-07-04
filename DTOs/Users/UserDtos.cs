using System.ComponentModel.DataAnnotations;

namespace LoginSystem.DTOs.Users;

public record CreateOrgAdminRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    [Required, RegularExpression(@"^\+[1-9]\d{1,14}$")] string PhoneNumber,
    string? Username,
    [Required] Guid OrganizationId);

public record CreateStudentRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    [Required, RegularExpression(@"^\+[1-9]\d{1,14}$")] string PhoneNumber,
    string? Username);

public record UserResponse(Guid Id, string Username, string Email, string Role, string Status, Guid? OrganizationId);
public record UpdateProfileRequest(string? PhoneNumber);