using System.Text.RegularExpressions;
using LoginSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace LoginSystem.Services;

public class UsernameGenerator : IUsernameGenerator
{
    private readonly AppDbContext _db;
    public UsernameGenerator(AppDbContext db) => _db = db;

    public async Task<string> GenerateFromEmailAsync(string email)
    {
        var local = email.Split('@')[0];
        var sanitized = Regex.Replace(local, @"[^a-zA-Z0-9._-]", "");
        if (string.IsNullOrWhiteSpace(sanitized)) sanitized = "user";

        var candidate = sanitized.ToLowerInvariant();
        var suffix = 1;
        while (await _db.Users.AnyAsync(u => u.Username == candidate))
            candidate = $"{sanitized.ToLowerInvariant()}{suffix++}";

        return candidate;
    }
}