namespace LoginSystem.Services
{
    public static class PasswordPolicy
    {
        public static bool IsValid(string password, out string? error)
        {
            if (password.Length < 8) { error = "Password must be at least 8 characters."; return false; }
            if (!password.Any(char.IsUpper)) { error = "Password must contain an uppercase letter."; return false; }
            if (!password.Any(char.IsLower)) { error = "Password must contain a lowercase letter."; return false; }
            if (!password.Any(char.IsDigit)) { error = "Password must contain a number."; return false; }
            if (!password.Any(c => !char.IsLetterOrDigit(c))) { error = "Password must contain a special character."; return false; }
            error = null;
            return true;
        }
    }
}
