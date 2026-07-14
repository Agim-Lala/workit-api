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
}
