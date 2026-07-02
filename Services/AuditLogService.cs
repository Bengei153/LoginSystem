using LoginSystem.Data;
using LoginSystem.Models;
namespace LoginSystem.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;
    public AuditLogService(AppDbContext db) => _db = db;

    public async Task LogAsync(string action, Guid? userId, Guid? organizationId, string? description, string? ipAddress, string? device)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            UserId = userId,
            OrganizationId = organizationId,
            Description = description,
            IpAddress = ipAddress,
            Device = device
        });
        await _db.SaveChangesAsync();
    }
}