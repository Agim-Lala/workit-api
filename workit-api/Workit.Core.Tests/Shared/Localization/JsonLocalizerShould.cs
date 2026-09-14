using System.Globalization;
using Shouldly;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Localization;

namespace Workit.Core.Tests.Shared.Localization;

public sealed class JsonLocalizerShould
{
    private readonly JsonLocalizer localizer = new();

    [Fact]
    public void TranslateKnownKeyIntoEnglishByDefault()
    {
        using var _ = new CultureScope("en");

        localizer["error.businessProfileNotFound"].ShouldBe("Business profile not found.");
    }

    [Fact]
    public void TranslateKnownKeyIntoAlbanianWhenCurrentCultureIsSq()
    {
        using var _ = new CultureScope("sq");

        localizer["error.businessProfileNotFound"].ShouldBe("Profili i biznesit nuk u gjet.");
        localizer.CurrentLanguage.ShouldBe("sq");
    }

    [Fact]
    public void FallBackToEnglishForUnknownAlbanianKey()
    {
        using var _ = new CultureScope("sq");

        localizer["error.title.validation"].ShouldNotBe("error.title.validation");
    }

    [Fact]
    public void ReturnKeyWhenNoTranslationExists()
    {
        localizer["totally.unknown.key"].ShouldBe("totally.unknown.key");
    }

    [Fact]
    public void ApplyFormatArguments()
    {
        using var _ = new CultureScope("en");

        localizer.Translate("validation.jobOpening.contentLanguageUnsupported", "de")
            .ShouldBe("'de' is not a supported content language.");
    }

    [Theory]
    [InlineData("en", JobType.ShortTerm, "Short-term")]
    [InlineData("sq", JobType.ShortTerm, "Afatshkurtër")]
    [InlineData("sq", ShiftType.CustomHours, "Orar i personalizuar")]
    public void LocalizeEnumLabels(string language, object value, string expected)
    {
        using var _ = new CultureScope(language);

        var label = value switch
        {
            JobType jobType => localizer.Enum(jobType),
            ShiftType shiftType => localizer.Enum(shiftType),
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
        };

        label.ShouldBe(expected);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo previous = CultureInfo.CurrentUICulture;

        public CultureScope(string language) =>
            CultureInfo.CurrentUICulture = new CultureInfo(language);

        public void Dispose() => CultureInfo.CurrentUICulture = previous;
    }
}
