namespace Workit.Core.Shared.Exceptions;

/// <summary>
/// Implemented by application exceptions whose user-facing message should be translated
/// by the API before it is written to the response. <see cref="LocalizationKey"/> is a
/// translation-catalog key; when it has no entry the key text is returned verbatim, so
/// passing a plain English sentence stays valid.
/// </summary>
public interface ILocalizableError
{
    string LocalizationKey { get; }

    object[] LocalizationArgs { get; }
}
