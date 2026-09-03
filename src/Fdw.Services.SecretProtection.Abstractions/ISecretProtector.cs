using Fdw.Results;

namespace Fdw.Services.SecretProtection.Abstractions;

/// <summary>
/// Encrypts and decrypts opaque bytes before they leave process memory.
/// </summary>
public interface ISecretProtector
{
    /// <summary>Encrypts <paramref name="plaintext"/>.</summary>
    /// <param name="plaintext">The bytes to encrypt.</param>
    IGenericResult<byte[]> Protect(byte[] plaintext);

    /// <summary>Decrypts <paramref name="ciphertext"/>.</summary>
    /// <param name="ciphertext">The bytes to decrypt.</param>
    IGenericResult<byte[]> Unprotect(byte[] ciphertext);
}
