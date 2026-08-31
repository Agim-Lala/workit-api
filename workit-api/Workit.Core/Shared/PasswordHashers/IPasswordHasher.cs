namespace Workit.Core.Shared.PasswordHashers;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
