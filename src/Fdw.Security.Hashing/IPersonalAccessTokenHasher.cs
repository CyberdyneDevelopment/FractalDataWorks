namespace Fdw.Security.Hashing;

/// <summary>Defines HMAC-SHA-256 hashing for Personal Access Tokens.</summary>
public interface IPersonalAccessTokenHasher
{
    /// <summary>Computes HMAC-SHA-256 hash of <paramref name="token"/> using <paramref name="hmacKey"/>. Returns lowercase 64-char hex.</summary>
    string Hash(string token, string hmacKey);

    /// <summary>Timing-safe comparison of <paramref name="token"/> against <paramref name="storedHash"/>.</summary>
    bool Verify(string token, string storedHash, string hmacKey);
}
