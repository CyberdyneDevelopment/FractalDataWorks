using System.Diagnostics.CodeAnalysis;

namespace Fdw.Security.Encryption;

/// <summary>
/// Encrypts <see cref="Plaintext"/> under the key named <see cref="SecretKeyName"/> in the secret
/// manager named <see cref="SecretManagerName"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record EncryptCommand
{
    /// <summary>Gets the secret manager the key is resolved from.</summary>
    public required string SecretManagerName { get; init; }

    /// <summary>Gets the key's name within that secret manager.</summary>
    public required string SecretKeyName { get; init; }

    /// <summary>Gets the bytes to encrypt.</summary>
    public required byte[] Plaintext { get; init; }
}
