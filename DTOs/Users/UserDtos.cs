using System.ComponentModel.DataAnnotations;

namespace LoginSystem.DTOs.Users;

public record CreateOrgAdminRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password,
    [property: Required, RegularExpression(@"^\+[1-9]\d{1,14}$")] string PhoneNumber,
    string? Username,
    [property: Required] Guid OrganizationId);

public record CreateStudentRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password,
    [property: Required, RegularExpression(@"^\+[1-9]\d{1,14}$")] string PhoneNumber,
    string? Username);

public record UserResponse(Guid Id, string Username, string Email, string Role, string Status, Guid? OrganizationId);
public record UpdateProfileRequest(string? PhoneNumber);