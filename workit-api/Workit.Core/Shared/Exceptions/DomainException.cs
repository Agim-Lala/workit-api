namespace Workit.Core.Shared.Exceptions;

public class DomainException : Exception, ILocalizableError
{
    public DomainException(string messageOrKey, params object[] args)
        : base(messageOrKey)
    {
        LocalizationKey = messageOrKey;
        LocalizationArgs = args;
    }

    public string LocalizationKey { get; }

    public object[] LocalizationArgs { get; }
}
