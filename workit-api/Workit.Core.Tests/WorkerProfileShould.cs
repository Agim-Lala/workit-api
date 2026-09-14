using Shouldly;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Tests.Workers.Domain;

public sealed class WorkerProfileShould
{
    [Fact]
    public void TrimLocationWhenCreatedAndChanged()
    {
        var profile = CreateProfile();

        profile.Location.ShouldBe("Tirana");

        profile.ChangeLocation("  Durrës  ");

        profile.Location.ShouldBe("Durrës");
    }

    [Fact]
    public void ClearVerificationWhenLocationChangedAsFreeText()
    {
        var profile = CreateProfile();
        profile.VerifyLocation("Tirana", "Albania", 41.3275, 19.8189);

        profile.ChangeLocation("Somewhere else");

        profile.IsLocationVerified.ShouldBeFalse();
        profile.Country.ShouldBeNull();
        profile.Latitude.ShouldBeNull();
        profile.Longitude.ShouldBeNull();
    }

    [Fact]
    public void StoreNormalizedFieldsWhenLocationVerified()
    {
        var profile = CreateProfile();

        profile.VerifyLocation("  Tirana  ", "  Albania  ", 41.3275, 19.8189);

        profile.Location.ShouldBe("Tirana");
        profile.Country.ShouldBe("Albania");
        profile.Latitude.ShouldBe(41.3275);
        profile.Longitude.ShouldBe(19.8189);
        profile.IsLocationVerified.ShouldBeTrue();
    }

    [Fact]
    public void StoreCvMetadataWhenSet()
    {
        var profile = CreateProfile();
        var uploadedAt = DateTimeOffset.UtcNow;

        profile.SetCv("worker-cvs/abc123.pdf", "  My CV.pdf  ", uploadedAt);

        profile.CvStorageKey.ShouldBe("worker-cvs/abc123.pdf");
        profile.CvOriginalFileName.ShouldBe("My CV.pdf");
        profile.CvUploadedAt.ShouldBe(uploadedAt);
    }

    [Fact]
    public void StorePhotoMetadataWhenSet()
    {
        var profile = CreateProfile();
        var uploadedAt = DateTimeOffset.UtcNow;

        profile.SetPhoto("worker-photos/abc123.jpg", uploadedAt);

        profile.PhotoStorageKey.ShouldBe("worker-photos/abc123.jpg");
        profile.PhotoUploadedAt.ShouldBe(uploadedAt);
    }

    [Fact]
    public void TrimAndDeduplicateInterestedFieldsWhenPreferencesSet()
    {
        var profile = CreateProfile();

        profile.SetPreferences(
            ["  Bartending  ", "Events", "bartending", ""],
            [ShiftType.Morning, ShiftType.Morning, ShiftType.Evening]);

        profile.InterestedFields.ShouldBe(["Bartending", "Events"]);
        profile.PreferredShiftTypes.ShouldBe([ShiftType.Morning, ShiftType.Evening]);
    }

    private static WorkerProfile CreateProfile()
    {
        return new WorkerProfile(
            Guid.NewGuid(),
            "Test",
            "Worker",
            "  Tirana  ",
            DateTimeOffset.UtcNow);
    }
}
