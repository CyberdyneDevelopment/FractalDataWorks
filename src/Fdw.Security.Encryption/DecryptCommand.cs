using System.Diagnostics.CodeAnalysis;

namespace Fdw.Security.Encryption;

/// <summary>
/// Decrypts <see cref="Ciphertext"/> using the key named <see cref="SecretKeyName"/> in the secret
/// manager named <see cref="SecretManagerName"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record DecryptCommand
{
    /// <summary>Gets the secret manager the key is resolved from.</summary>
    public required string SecretManagerName { get; init; }

    /// <summary>Gets the key's name within that secret manager.</summary>
    public required string SecretKeyName { get; init; }

    /// <summary>Gets the bytes produced by a matching <see cref="EncryptCommand"/>.</summary>
    public required byte[] Ciphertext { get; init; }
}
