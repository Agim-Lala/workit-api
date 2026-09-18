using Shouldly;
using Workit.Core.Users.Domain;

namespace Workit.Core.Tests.Users.Domain;

public sealed class UserShould
{
    [Fact]
    public void NormalizeEmailWhenCreated()
    {
        var user = new User(
            "  USER@Example.COM  ",
            "password-hash",
            DateTimeOffset.UtcNow);

        user.Email.ShouldBe("user@example.com");
    }

    [Fact]
    public void NormalizeEmail()
    {
        var email = User.NormalizeEmail("  USER@Example.COM  ");

        email.ShouldBe("user@example.com");
    }

    [Fact]
    public void StartWithEmailUnconfirmed()
    {
        var user = new User("user@example.com", "password-hash", DateTimeOffset.UtcNow);

        user.EmailConfirmed.ShouldBeFalse();
    }

    [Fact]
    public void ConsiderConfirmationTokenValidWhileUnexpiredAndMatching()
    {
        var user = new User("user@example.com", "password-hash", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        user.SetEmailConfirmationToken("token-hash", now.AddHours(1));

        user.HasValidEmailConfirmationToken("token-hash", now).ShouldBeTrue();
        user.HasValidEmailConfirmationToken("wrong-hash", now).ShouldBeFalse();
        user.HasValidEmailConfirmationToken("token-hash", now.AddHours(2)).ShouldBeFalse();
    }

    [Fact]
    public void ClearTokenWhenEmailConfirmed()
    {
        var user = new User("user@example.com", "password-hash", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        user.SetEmailConfirmationToken("token-hash", now.AddHours(1));

        user.ConfirmEmail();

        user.EmailConfirmed.ShouldBeTrue();
        user.EmailConfirmationTokenHash.ShouldBeNull();
        user.EmailConfirmationTokenExpiresAt.ShouldBeNull();
        user.HasValidEmailConfirmationToken("token-hash", now).ShouldBeFalse();
    }
}
