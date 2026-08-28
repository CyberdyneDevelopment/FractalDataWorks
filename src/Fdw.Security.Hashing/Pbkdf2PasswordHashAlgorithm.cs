using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Fdw.Security.Hashing;

/// <summary>
/// PBKDF2-HMAC-SHA512 password hash algorithm with separate salt storage.
/// </summary>
/// <remarks>
/// Uses 128-bit (16-byte) random salt and 256-bit (32-byte) derived key.
/// 210,000 iterations of PBKDF2 with HMAC-SHA512.
/// Salt and hash are stored separately (not embedded like Identity v3 format).
/// This is the EDGE KDF (run on credential arrival); the vault adds the pepper and does the compare.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PasswordHashAlgorithms), "Pbkdf2")]
public sealed class Pbkdf2PasswordHashAlgorithm : PasswordHashAlgorithmBase
{
    private const int IterationCount = 210_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private static readonly KeyDerivationPrf Prf = KeyDerivationPrf.HMACSHA512;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pbkdf2PasswordHashAlgorithm"/> class.
    /// </summary>
    public Pbkdf2PasswordHashAlgorithm() : base(1, "Pbkdf2")
    {
    }

    /// <inheritdoc />
    public override PasswordHashResult HashPassword(string plaintext)
    {
        var saltBytes = new byte[SaltSizeBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }

        var hashBytes = KeyDerivation.Pbkdf2(
            password: plaintext,
            salt: saltBytes,
            prf: Prf,
            iterationCount: IterationCount,
            numBytesRequested: HashSizeBytes);

        return new PasswordHashResult(
            hash: Convert.ToBase64String(hashBytes),
            salt: Convert.ToBase64String(saltBytes),
            algorithmName: Name);
    }

    /// <inheritdoc />
    public override byte[] DeriveKey(string plaintext, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        return KeyDerivation.Pbkdf2(
            password: plaintext,
            salt: saltBytes,
            prf: Prf,
            iterationCount: IterationCount,
            numBytesRequested: HashSizeBytes);
    }

    /// <inheritdoc />
    public override bool VerifyPassword(string plaintext, string storedHash, string storedSalt)
    {
        byte[] saltBytes;
        byte[] expectedHash;

        try
        {
            saltBytes = Convert.FromBase64String(storedSalt);
            expectedHash = Convert.FromBase64String(storedHash);
        }
        catch (FormatException ex)
        {
            _ = ex;
            return false;
        }

        var actualHash = KeyDerivation.Pbkdf2(
            password: plaintext,
            salt: saltBytes,
            prf: Prf,
            iterationCount: IterationCount,
            numBytesRequested: expectedHash.Length);

        return FixedTimeEquals(actualHash, expectedHash);
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;

        var diff = 0;
        for (var i = 0; i < left.Length; i++)
        {
            diff |= left[i] ^ right[i];
        }

        return diff == 0;
    }
}
