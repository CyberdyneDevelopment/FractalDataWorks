using System;
using System.Security.Cryptography;
using System.Text;

namespace Fdw.Security.Hashing;

/// <summary>HMAC-SHA-256 hasher for Personal Access Tokens.</summary>
public sealed class PersonalAccessTokenHasher : IPersonalAccessTokenHasher
{
    /// <inheritdoc/>
    public string Hash(string token, string hmacKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(hmacKey);
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(tokenBytes);
        return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <inheritdoc/>
    public bool Verify(string token, string storedHash, string hmacKey)
    {
        var computed = Hash(token, hmacKey);
        return FixedTimeEquals(computed, storedHash);
    }

    // Manual fixed-time comparison (CryptographicOperations.FixedTimeEquals requires netstandard2.1+)
    private static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length)
            return false;
        var result = 0;
        for (var i = 0; i < a.Length; i++)
            result |= a[i] ^ b[i];
        return result == 0;
    }
}
