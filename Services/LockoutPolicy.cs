using LoginSystem.Models;

namespace LoginSystem.Services;

public static class LockoutPolicy
{
    public static bool IsLockedOut(User user) =>
        user.Status == AccountStatus.Locked && user.LockoutEnd is { } end && end > DateTime.UtcNow;

    public static void RegisterFailedAttempt(User user, int maxAttempts, TimeSpan lockoutDuration)
    {
        user.FailedLoginCount++;
        if (user.FailedLoginCount >= maxAttempts)
        {
            user.Status = AccountStatus.Locked;
            user.LockoutEnd = DateTime.UtcNow.Add(lockoutDuration);
        }
    }

    public static void RegisterSuccessfulLogin(User user)
    {
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        if (user.Status == AccountStatus.Locked)
            user.Status = AccountStatus.Active; // lockout window's passed, reinstate on success
    }
}