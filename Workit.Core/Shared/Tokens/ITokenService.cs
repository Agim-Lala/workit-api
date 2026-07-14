namespace Workit.Core.Shared.Tokens;

public interface ITokenService
{
    public (string PlainToken, string TokenHash) GenerateToken();
    public bool VerifyToken(string plainToken, string tokenHash);
    public (string PlainCode, string CodeHash) GenerateVerificationCode();
}
