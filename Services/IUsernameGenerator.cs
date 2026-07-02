namespace LoginSystem.Services;

public interface IUsernameGenerator { Task<string> GenerateFromEmailAsync(string email); }