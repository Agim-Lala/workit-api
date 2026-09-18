using Shouldly;
using Workit.Core.Businesses.Domain;

namespace Workit.Core.Tests.Businesses.Domain;

public sealed class BusinessProfileShould
{
    [Fact]
    public void NormalizeNiptWhenCreated()
    {
        var businessProfile = new BusinessProfile(
            Guid.NewGuid(),
            "Test Business",
            "Rruga Test, Tirane",
            41.3275m,
            19.8189m,
            "  k12345678a  ",
            DateTimeOffset.UtcNow);

        businessProfile.Nipt.ShouldBe("K12345678A");
    }

    [Fact]
    public void NormalizeNipt()
    {
        var nipt = BusinessProfile.NormalizeNipt("  k12345678a  ");

        nipt.ShouldBe("K12345678A");
    }
}
