namespace LoginSystem.Models
{
    public class Organization
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? Address { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }   // Super Admin's User.Id — no FK constraint, just a reference
        public bool IsActive { get; set; } = true;
        public DateTime? DeletedAt { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
