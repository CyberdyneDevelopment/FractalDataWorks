using Fdw.Collections;

namespace Fdw.Security.Hashing;

/// <summary>
/// Base class for password hash algorithm TypeOptions.
/// </summary>
public abstract class PasswordHashAlgorithmBase
    : TypeOptionBase<int, PasswordHashAlgorithmBase>, IPasswordHashAlgorithm
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordHashAlgorithmBase"/> class.
    /// </summary>
    /// <param name="id">The unique numeric identifier for this algorithm.</param>
    /// <param name="name">The name of this algorithm (e.g., "Pbkdf2", "Argon2").</param>
    protected PasswordHashAlgorithmBase(int id, string name) : base(id, name)
    {
    }

    /// <inheritdoc />
    public abstract PasswordHashResult HashPassword(string plaintext);

    /// <inheritdoc />
    public abstract bool VerifyPassword(string plaintext, string storedHash, string storedSalt);

    /// <inheritdoc />
    public abstract byte[] DeriveKey(string plaintext, string storedSalt);
}
