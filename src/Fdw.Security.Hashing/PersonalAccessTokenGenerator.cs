using System;
using System.Security.Cryptography;
using System.Text;

namespace Fdw.Security.Hashing;

/// <summary>Generates Personal Access Tokens using cryptographically random bytes encoded in base-62.</summary>
public sealed class PersonalAccessTokenGenerator : IPersonalAccessTokenGenerator
{
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int DisplayPrefixLength = 20;

    /// <inheritdoc/>
    public string Generate(string environment)
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var suffix = ToBase62(randomBytes);
        return $"fdx_{environment}_{suffix}";
    }

    /// <inheritdoc/>
    public string ExtractPrefix(string token)
        => token.Length <= DisplayPrefixLength ? token : token.Substring(0, DisplayPrefixLength);

    private static string ToBase62(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
            sb.Append(Base62Chars[b % 62]);
        return sb.ToString();
    }
}
