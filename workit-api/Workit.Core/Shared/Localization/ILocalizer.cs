namespace Workit.Core.Shared.Localization;

/// <summary>
/// Resolves user-facing text for the language of the current request.
/// The active language is taken from <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>,
/// which the API sets from the <c>Accept-Language</c> header.
/// </summary>
public interface ILocalizer
{
    /// <summary>The resolved two-letter language code for the current request (for example <c>en</c> or <c>sq</c>).</summary>
    string CurrentLanguage { get; }

    /// <summary>Translates <paramref name="key"/>, returning the key itself when no translation exists.</summary>
    string this[string key] { get; }

    /// <summary>Translates <paramref name="key"/> and applies <see cref="string.Format(string, object?[])"/> with <paramref name="args"/>.</summary>
    string Translate(string key, params object[] args);

    /// <summary>Translates for an explicit language instead of the current request language.</summary>
    string TranslateFor(string language, string key, params object[] args);

    /// <summary>Localized display label for an enum value, keyed as <c>enum.{EnumType}.{Value}</c>.</summary>
    string Enum<TEnum>(TEnum value) where TEnum : struct, System.Enum;
}
