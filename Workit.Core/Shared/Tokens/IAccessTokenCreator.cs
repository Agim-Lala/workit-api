using Workit.Core.Users.Domain;

namespace Workit.Core.Shared.Tokens;

public interface IAccessTokenCreator
{
    string Create(User user, DateTimeOffset expiresAt);
}
