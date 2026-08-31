using Shouldly;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Tests.Workers.Domain;

public sealed class WorkerProfileShould
{
    [Fact]
    public void TrimLocationWhenCreatedAndChanged()
    {
        var profile = new WorkerProfile(
            Guid.NewGuid(),
            "Test",
            "Worker",
            "  Tirana  ",
            DateTimeOffset.UtcNow);

        profile.Location.ShouldBe("Tirana");

        profile.ChangeLocation("  Durrës  ");

        profile.Location.ShouldBe("Durrës");
    }
}
