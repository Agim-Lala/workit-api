namespace Workit.Core.JobOpenings.Domain;

/// <summary>
/// A business-supplied translation of a job opening's free-text fields for one language.
/// Any field left null falls back to the job opening's base-language value on read.
/// </summary>
public sealed record JobOpeningTranslation(
    string? Title,
    string? Description,
    string? Role);

/// <summary>The free-text content of a job opening resolved for a single language.</summary>
public readonly record struct JobOpeningContent(
    string Title,
    string Description,
    string Role);
