using System.ComponentModel.DataAnnotations;

namespace LoginSystem.DTOs.Organizations;

public record CreateOrganizationRequest(
    [property: Required, MinLength(2)] string Name,
    [property: Required, RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Lowercase letters, numbers, hyphens only.")] string Slug,
    [property: Required, EmailAddress] string ContactEmail,
    string? ContactPhone,
    string? Description);

public record OrganizationResponse(Guid Id, string Name, string Slug, bool IsActive, DateTime CreatedAt);
public record UpdateOrganizationRequest(string? Name, string? Description, string? LogoUrl, string? Address, string? ContactEmail, string? ContactPhone);