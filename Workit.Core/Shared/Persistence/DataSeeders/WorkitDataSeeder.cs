using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.PasswordHashers;
using Workit.Core.Shared.Time;
using Workit.Core.Users.Domain;

namespace Workit.Core.Shared.Persistence.DataSeeders;

public sealed class WorkitDataSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IClock clock)
    : IDataSeeder
{
    private const string SeedPassword = "Test@1234";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedUserAsync("user@workit.al", UserRole.User, cancellationToken);
        await SeedUserAsync("admin@workit.al", UserRole.Admin, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUserAsync(
        string email,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        var userExists = await db.Set<User>()
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (userExists)
        {
            return;
        }

        db.Set<User>().Add(new User(
            normalizedEmail,
            passwordHasher.Hash(SeedPassword),
            clock.UtcNow,
            role));
    }
}
