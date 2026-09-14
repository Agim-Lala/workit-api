namespace Workit.Core.Shared.Localization;

/// <summary>
/// Supported content and message languages for the Workit API.
/// </summary>
public static class Language
{
    public const string English = "en";
    public const string Albanian = "sq";

    public const string Default = English;

    public static readonly IReadOnlyList<string> Supported = [English, Albanian];

    public static bool IsSupported(string? language)
    {
        return language is not null
            && Supported.Contains(Normalize(language));
    }

    /// <summary>
    /// Reduces a culture name such as <c>sq-AL</c> to its two-letter code and
    /// falls back to <see cref="Default"/> when the language is not supported.
    /// </summary>
    public static string Resolve(string? language)
    {
        var normalized = Normalize(language);
        return Supported.Contains(normalized) ? normalized : Default;
    }

    private static string Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return Default;
        }

        var trimmed = language.Trim();
        var separator = trimmed.IndexOfAny(['-', '_']);
        var twoLetter = separator > 0 ? trimmed[..separator] : trimmed;
        return twoLetter.ToLowerInvariant();
    }
}
