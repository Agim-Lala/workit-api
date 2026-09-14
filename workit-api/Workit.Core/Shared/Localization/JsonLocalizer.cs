using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Workit.Core.Shared.Localization;

/// <summary>
/// <see cref="ILocalizer"/> backed by flat <c>key: value</c> JSON resources embedded in this assembly
/// (<c>Shared/Localization/Resources/translations.{lang}.json</c>). Resources are read once on construction.
/// </summary>
public sealed class JsonLocalizer : ILocalizer
{
    private const string ResourceNamespace = "Workit.Core.Shared.Localization.Resources.translations.";

    private readonly FrozenDictionary<string, FrozenDictionary<string, string>> catalogs;

    public JsonLocalizer()
        : this(typeof(JsonLocalizer).Assembly)
    {
    }

    internal JsonLocalizer(Assembly assembly)
    {
        catalogs = Language.Supported
            .ToFrozenDictionary(
                language => language,
                language => LoadCatalog(assembly, language));
    }

    public string CurrentLanguage => Language.Resolve(CultureInfo.CurrentUICulture.Name);

    public string this[string key] => Translate(key);

    public string Translate(string key, params object[] args) =>
        TranslateFor(CurrentLanguage, key, args);

    public string TranslateFor(string language, string key, params object[] args)
    {
        var resolvedLanguage = Language.Resolve(language);
        var value = Lookup(resolvedLanguage, key)
            ?? Lookup(Language.Default, key)
            ?? key;

        if (args.Length == 0)
        {
            return value;
        }

        try
        {
            return string.Format(CultureInfo.InvariantCulture, value, args);
        }
        catch (FormatException)
        {
            return value;
        }
    }

    public string Enum<TEnum>(TEnum value) where TEnum : struct, System.Enum =>
        Translate($"enum.{typeof(TEnum).Name}.{value}");

    private string? Lookup(string language, string key) =>
        catalogs.TryGetValue(language, out var catalog) && catalog.TryGetValue(key, out var value)
            ? value
            : null;

    private static FrozenDictionary<string, string> LoadCatalog(Assembly assembly, string language)
    {
        var resourceName = ResourceNamespace + language + ".json";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded translation resource '{resourceName}'.");

        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException($"Translation resource '{resourceName}' is empty or invalid.");

        return entries.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
