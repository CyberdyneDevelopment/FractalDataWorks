using Fdw.Collections;

namespace Fdw.Security.Hashing;

/// <summary>
/// A password hashing algorithm that produces a hash and salt separately.
/// TypeCollection member — implementations are discovered and registered automatically.
/// </summary>
// Why: TypeCollection allows configuring which algorithm is active and enables
// future migration (e.g., PBKDF2 → Argon2) without code changes.
public interface IPasswordHashAlgorithm : ITypeOption<int, PasswordHashAlgorithmBase>
{
    /// <summary>
    /// Hashes a plaintext password, producing a hash and salt separately.
    /// </summary>
    /// <param name="plaintext">The plaintext password.</param>
    /// <returns>A result containing the hash, salt, and algorithm name.</returns>
    PasswordHashResult HashPassword(string plaintext);

    /// <summary>
    /// Verifies a plaintext password against a stored hash and salt.
    /// </summary>
    /// <param name="plaintext">The plaintext password to verify.</param>
    /// <param name="storedHash">The stored hash (Base64-encoded).</param>
    /// <param name="storedSalt">The stored salt (Base64-encoded).</param>
    /// <returns>True if the password matches; otherwise, false.</returns>
    bool VerifyPassword(string plaintext, string storedHash, string storedSalt);

    /// <summary>
    /// Derives the raw key bytes from a plaintext over a KNOWN salt — the edge KDF used by the
    /// credential service to hash-on-arrival. No comparison happens here; the derived bytes are handed
    /// to the vault, which adds the pepper and performs the constant-time compare/store.
    /// </summary>
    /// <param name="plaintext">The plaintext credential.</param>
    /// <param name="storedSalt">The salt to derive against (Base64-encoded).</param>
    /// <returns>The raw derived key bytes.</returns>
    byte[] DeriveKey(string plaintext, string storedSalt);
}
