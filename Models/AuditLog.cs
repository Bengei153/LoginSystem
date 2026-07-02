namespace LoginSystem.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; }          // nullable — e.g. failed login with an unknown username
        public Guid? OrganizationId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IpAddress { get; set; }
        public string? Device { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
