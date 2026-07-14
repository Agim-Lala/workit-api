namespace Workit.Core.Users.Domain;

public sealed class User
{
    public const int MaxEmailLength = 320;
    public const int MaxPasswordHashLength = 256;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private User()
    {
    }

    public User(string email, string passwordHash, DateTimeOffset createdAt)
    {
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
