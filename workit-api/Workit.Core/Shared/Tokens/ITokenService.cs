namespace Workit.Core.Shared.Tokens;

public interface ITokenService
{
    public (string PlainToken, string TokenHash) GenerateToken();
    public bool VerifyToken(string plainToken, string tokenHash);
    public (string PlainCode, string CodeHash) GenerateVerificationCode();

    /// <summary>Deterministically hashes a plain token, so a caller can look up a record by hash
    /// (e.g. finding the user a confirmation link's token belongs to) before verifying it.</summary>
    public string HashToken(string plainToken);
}
