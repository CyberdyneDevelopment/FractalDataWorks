using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Identity;

namespace Fdw.Security.Hashing;

/// <summary>
/// Password hasher delegating to ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/>.
/// </summary>
/// <remarks>
/// Uses PBKDF2-HMAC-SHA512 with a 128-bit random salt (Identity v3 format).
/// Salt is embedded in the hash output. No separate pepper is needed because
/// Identity already uses a high iteration count and cryptographically random salt.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return _inner.HashPassword(null!, password);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hash)
    {
        var result = _inner.VerifyHashedPassword(null!, hash, password);
        return result != PasswordVerificationResult.Failed;
    }
}
