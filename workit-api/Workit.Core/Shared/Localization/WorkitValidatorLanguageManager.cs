using System.Globalization;
using FluentValidation.Resources;

namespace Workit.Core.Shared.Localization;

/// <summary>
/// Routes FluentValidation's built-in messages (<c>NotEmptyValidator</c>, <c>EmailValidator</c>, ...)
/// through the Workit translation catalog under the <c>validation.</c> prefix, so English and Albanian
/// messages stay in one place. Unknown keys fall back to FluentValidation's own resources.
/// </summary>
public sealed class WorkitValidatorLanguageManager(ILocalizer localizer) : LanguageManager
{
    public override string GetString(string key, CultureInfo? culture = null)
    {
        var catalogKey = $"validation.{key}";
        var translated = localizer.Translate(catalogKey);
        return translated == catalogKey
            ? base.GetString(key, culture)
            : translated;
    }
}
