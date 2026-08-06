namespace Fdw.Security.Hashing;

/// <summary>
/// Result of a password hashing operation.
/// Contains the hash, salt, and algorithm name — all stored separately.
/// </summary>
public sealed class PasswordHashResult
{
    /// <summary>
    /// Gets the hashed password (Base64-encoded).
    /// </summary>
    public string Hash { get; }

    /// <summary>
    /// Gets the salt used for hashing (Base64-encoded).
    /// </summary>
    public string Salt { get; }

    /// <summary>
    /// Gets the algorithm name used to produce this hash.
    /// </summary>
    // Why: Stored alongside the hash so the correct algorithm can be used
    // for verification even after the system default changes.
    public string AlgorithmName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordHashResult"/> class.
    /// </summary>
    public PasswordHashResult(string hash, string salt, string algorithmName)
    {
        Hash = hash;
        Salt = salt;
        AlgorithmName = algorithmName;
    }
}
