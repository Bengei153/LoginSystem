namespace LoginSystem.Services;
public interface IAuditLogService
{
    Task LogAsync(string action, Guid? userId, Guid? organizationId, string? description, string? ipAddress, string? device);
}