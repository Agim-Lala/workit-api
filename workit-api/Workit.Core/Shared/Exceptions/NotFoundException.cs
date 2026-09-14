namespace Workit.Core.Shared.Exceptions;

public class NotFoundException : Exception, ILocalizableError
{
    public NotFoundException(string messageOrKey, params object[] args)
        : base(messageOrKey)
    {
        LocalizationKey = messageOrKey;
        LocalizationArgs = args;
    }

    public string LocalizationKey { get; }

    public object[] LocalizationArgs { get; }
}
