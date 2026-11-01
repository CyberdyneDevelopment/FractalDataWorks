using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Security.Encryption;

/// <summary>
/// Runs <see cref="EncryptCommand"/>/<see cref="DecryptCommand"/> against whatever algorithm and key
/// source this host is configured with.
/// </summary>
/// <remarks>
/// Every domain that needs at-rest confidentiality for a blob it already owns depends on this rather
/// than reimplementing key resolution — <c>ISecretManagerProvider</c> already solves "hold and hand
/// back secret material"; this is the one place that turns that material into ciphertext.
/// </remarks>
public interface IEncryptionCommandExecutor
{
    /// <summary>Runs <paramref name="command"/> and returns the resulting ciphertext.</summary>
    /// <param name="command">What to encrypt and which key to use.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<byte[]>> Execute(EncryptCommand command, CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="command"/> and returns the recovered plaintext.</summary>
    /// <param name="command">What to decrypt and which key to use.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<byte[]>> Execute(DecryptCommand command, CancellationToken cancellationToken = default);
}
