using System.ComponentModel.DataAnnotations.Schema;

namespace LoginSystem.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? Device { get; set; }

        [NotMapped]
        public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
    }
}
