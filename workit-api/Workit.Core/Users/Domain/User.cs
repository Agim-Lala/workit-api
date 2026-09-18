namespace Workit.Core.Users.Domain;

public sealed class User
{
    public const int MaxEmailLength = 320;
    public const int MaxPasswordHashLength = 256;
    public const int MaxRoleLength = 32;
    public const int MaxEmailConfirmationTokenHashLength = 64;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Worker;
    public bool EmailConfirmed { get; private set; }
    public string? EmailConfirmationTokenHash { get; private set; }
    public DateTimeOffset? EmailConfirmationTokenExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private User()
    {
    }

    public User(
        string email,
        string passwordHash,
        DateTimeOffset createdAt,
        UserRole role = UserRole.Worker)
    {
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        Role = role;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>Issues a fresh confirmation token, replacing any previous one.</summary>
    public void SetEmailConfirmationToken(string tokenHash, DateTimeOffset expiresAt)
    {
        EmailConfirmationTokenHash = tokenHash;
        EmailConfirmationTokenExpiresAt = expiresAt;
    }

    public bool HasValidEmailConfirmationToken(string tokenHash, DateTimeOffset now)
    {
        return EmailConfirmationTokenHash == tokenHash
            && EmailConfirmationTokenExpiresAt is not null
            && EmailConfirmationTokenExpiresAt > now;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        EmailConfirmationTokenHash = null;
        EmailConfirmationTokenExpiresAt = null;
    }
}
