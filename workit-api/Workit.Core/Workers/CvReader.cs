using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Workit.Core.Workers;

/// <summary>
/// Reads uploaded CV PDFs into plain text and derives an internal, keyword-based profile from
/// it (roles, languages, years of experience). Internal only: used for ranking, never returned
/// to workers or businesses.
/// </summary>
public static class CvReader
{
    public sealed record Insights(IReadOnlyList<string> Roles, IReadOnlyList<string> Languages, int? YearsOfExperience);

    // Canonical role -> English/Albanian keywords, matched diacritic-insensitively as word prefixes
    // so "kamarier" also hits "kamarierja". A trailing "." means whole word only, for short English
    // words that prefix unrelated ones ("cook." skips "cookie"). ponytail: fixed hospitality-focused
    // list; swap for an LLM extraction if precision matters.
    private static readonly IReadOnlyDictionary<string, Regex> RoleKeywords = Compile(new Dictionary<string, string[]>
    {
        ["waiter"] = ["waiter", "waitress", "server", "kamarier"],
        ["bartender"] = ["bartender", "bartending", "barman", "banakier"],
        ["barista"] = ["barista"],
        ["chef"] = ["chef", "cook.", "cooks.", "kuzhinier"],
        ["kitchen-assistant"] = ["kitchen assistant", "kitchen porter", "dishwasher", "ndihmës kuzhinier", "pjatalarës"],
        ["pastry"] = ["pastry", "pastiçier", "pastiqier", "ëmbëltore"],
        ["receptionist"] = ["receptionist", "front desk", "recepsionist"],
        ["housekeeping"] = ["housekeeping", "housekeeper", "room attendant", "cleaner", "cleaning", "pastrues", "pastruese"],
        ["host"] = ["host.", "hosts.", "hostess"],
        ["cashier"] = ["cashier", "arkëtar"],
        ["sales"] = ["sales", "shop assistant", "shitës"],
        ["driver"] = ["driver", "delivery", "courier", "shofer", "korrier"],
        ["security"] = ["security", "bouncer", "sigurim"],
        ["event-staff"] = ["event.", "events.", "catering", "banquet", "eveniment"],
        ["warehouse"] = ["warehouse", "magazinier"],
    });

    private static readonly IReadOnlyDictionary<string, Regex> LanguageKeywords = Compile(new Dictionary<string, string[]>
    {
        ["albanian"] = ["albanian", "shqip"],
        ["english"] = ["english", "anglisht"],
        ["italian"] = ["italian", "italisht"],
        ["german"] = ["german", "gjermanisht"],
        ["french"] = ["french", "frëngjisht"],
        ["spanish"] = ["spanish", "spanjisht"],
        ["greek"] = ["greek", "greqisht"],
        ["turkish"] = ["turkish", "turqisht"],
    });

    private const int MaxYearsOfExperience = 50;
    // "(?!\s*old)" skips ages ("25 years old").
    private static readonly Regex YearsPattern = new(
        @"\b(\d{1,2})\s*\+?\s*(years?|yrs?|vjet|vite)\b(?!\s*old)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Pulls plain text out of a PDF. Returns null for PDFs that can't be parsed or have no text
    /// layer (scans) — the upload itself still succeeds.
    /// </summary>
    public static string? ExtractText(byte[] pdf)
    {
        try
        {
            using var document = PdfDocument.Open(pdf);
            // page.Text often drops inter-word spaces; the layout-aware extractor keeps them. NULs
            // are stripped because Postgres rejects them in text columns.
            var text = string.Join('\n', document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page))).Replace("\0", "");
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static Insights Analyze(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new Insights([], [], null);
        }

        text = Normalize(text);

        // Largest "N years" mention, as CVs usually state total experience once and per-job
        // durations elsewhere.
        var years = YearsPattern.Matches(text)
            .Select(match => int.Parse(match.Groups[1].Value))
            .Where(value => value <= MaxYearsOfExperience)
            .DefaultIfEmpty()
            .Max();

        return new Insights(Match(text, RoleKeywords), Match(text, LanguageKeywords), years == 0 ? null : years);
    }

    /// <summary>Canonical roles a job opening's free-text role maps to, e.g. "Kamarier" -> ["waiter"].</summary>
    public static IReadOnlyList<string> RolesIn(string text) => Match(Normalize(text), RoleKeywords);

    private static List<string> Match(string normalizedText, IReadOnlyDictionary<string, Regex> patterns) =>
        patterns.Where(entry => entry.Value.IsMatch(normalizedText)).Select(entry => entry.Key).ToList();

    private static Dictionary<string, Regex> Compile(Dictionary<string, string[]> keywords) =>
        keywords.ToDictionary(
            entry => entry.Key,
            entry => new Regex(
                $@"\b(?:{string.Join('|', entry.Value.Select(keyword => keyword.EndsWith('.')
                    ? $@"{Regex.Escape(Normalize(keyword[..^1]))}\b"
                    : $@"{Regex.Escape(Normalize(keyword))}\w*"))})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled));

    // Strips diacritics ("ë" -> "e", "ç" -> "c") since Albanian CVs are often typed without them.
    private static string Normalize(string text) =>
        new string(text.Normalize(NormalizationForm.FormD)
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .ToArray());
}
