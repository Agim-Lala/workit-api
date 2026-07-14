using System.Security.Cryptography;
using System.Text;

namespace Workit.Core.Shared.Tokens;

public sealed class TokenService : ITokenService
{
    public (string PlainToken, string TokenHash) GenerateToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var plainToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');

        var tokenHash = HashToken(plainToken);

        return (plainToken, tokenHash);
    }

    public (string PlainCode, string CodeHash) GenerateVerificationCode()
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = HashToken(code);

        return (code, codeHash);
    }

    public bool VerifyToken(string plainToken, string tokenHash)
    {
        var computedHash = HashToken(plainToken);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(tokenHash));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
