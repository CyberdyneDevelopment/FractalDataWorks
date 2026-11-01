using System;
using System.Security.Cryptography;
using System.Text;

namespace Fdw.Services.Pipelines.Webhooks;

/// <summary>
/// Makes a webhook trigger's secret and checks one (FDW-1884). A secret is 32 random bytes as URL-safe text; only its SHA-256 is stored.
/// </summary>
/// <remarks>
/// Why a plain SHA-256 and no slow hash or server key: the secret is 256 random bits, not something a person chose, so there is nothing to
/// guess and nothing to stretch, and no key to hold that could be missing. The check compares the hashes in constant time.
/// </remarks>
public static class WebhookSecret
{
    private const int SecretBytes = 32;
    private const int PrefixLength = 8;

    /// <summary>Makes a new secret.</summary>
    /// <returns>The secret, to be shown once; its hash, to be stored; and its first characters, to be shown with the trigger.</returns>
    public static (string Secret, string Hash, string Prefix) Create()
    {
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(SecretBytes))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return (secret, Hash(secret), secret[..PrefixLength]);
    }

    /// <summary>Hashes a secret.</summary>
    /// <param name="secret">The secret.</param>
    /// <returns>The SHA-256 of its UTF-8 bytes as lowercase hexadecimal.</returns>
    public static string Hash(string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }

    /// <summary>Checks a secret a caller sent against the stored hash.</summary>
    /// <param name="sent">The secret the caller sent; null or empty never matches.</param>
    /// <param name="storedHash">The stored hash.</param>
    /// <returns><see langword="true"/> when the secret hashes to the stored hash.</returns>
    public static bool Matches(string? sent, string storedHash)
    {
        ArgumentNullException.ThrowIfNull(storedHash);
        if (string.IsNullOrEmpty(sent))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(sent)), Encoding.ASCII.GetBytes(storedHash));
    }
}
