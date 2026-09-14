using Shouldly;
using Workit.Core.JobOpenings.Domain;

namespace Workit.Core.Tests.JobOpenings.Domain;

public sealed class JobOpeningContentResolutionShould
{
    [Fact]
    public void ReturnBaseContentForTheBaseLanguage()
    {
        var jobOpening = Create(translations: new Dictionary<string, JobOpeningTranslation>
        {
            ["sq"] = new("Titull", "Përshkrim", "Rol"),
        });

        var content = jobOpening.ResolveContent("en");

        content.Title.ShouldBe("Waiter needed");
        content.Description.ShouldBe("Evening service.");
        content.Role.ShouldBe("Waiter");
    }

    [Fact]
    public void ReturnTranslatedContentForAMatchingLanguage()
    {
        var jobOpening = Create(translations: new Dictionary<string, JobOpeningTranslation>
        {
            ["sq"] = new("Kërkohet kamarier", "Shërbim mbrëmjeje.", "Kamarier"),
        });

        var content = jobOpening.ResolveContent("sq-AL");

        content.Title.ShouldBe("Kërkohet kamarier");
        content.Description.ShouldBe("Shërbim mbrëmjeje.");
        content.Role.ShouldBe("Kamarier");
    }

    [Fact]
    public void FallBackFieldByFieldWhenATranslatedFieldIsMissing()
    {
        var jobOpening = Create(translations: new Dictionary<string, JobOpeningTranslation>
        {
            ["sq"] = new("Kërkohet kamarier", null, "   "),
        });

        var content = jobOpening.ResolveContent("sq");

        content.Title.ShouldBe("Kërkohet kamarier");
        content.Description.ShouldBe("Evening service.");
        content.Role.ShouldBe("Waiter");
    }

    [Fact]
    public void FallBackToBaseContentForAnUntranslatedLanguage()
    {
        var jobOpening = Create(translations: null);

        jobOpening.ResolveContent("sq").Title.ShouldBe("Waiter needed");
    }

    [Fact]
    public void DropTranslationEntriesThatMatchTheBaseLanguage()
    {
        var jobOpening = Create(
            contentLanguage: "sq",
            translations: new Dictionary<string, JobOpeningTranslation>
            {
                ["sq"] = new("ndryshe", "ndryshe", "ndryshe"),
            });

        jobOpening.Translations.ShouldNotContainKey("sq");
    }

    private static JobOpening Create(
        string? contentLanguage = null,
        IReadOnlyDictionary<string, JobOpeningTranslation>? translations = null)
    {
        return new JobOpening(
            Guid.NewGuid(),
            "Waiter needed",
            "Evening service.",
            "Waiter",
            "Tirana",
            8.5m,
            PayType.Hourly,
            JobType.ShortTerm,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 2),
            ShiftType.Evening,
            null,
            null,
            2,
            DateTimeOffset.UtcNow,
            contentLanguage,
            translations);
    }
}
