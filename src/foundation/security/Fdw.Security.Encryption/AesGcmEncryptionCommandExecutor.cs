using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Security.Encryption.Logging;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Security.Encryption;

/// <summary>
/// AES-256-GCM implementation of <see cref="IEncryptionCommandExecutor"/>. The key is resolved by
/// name from whichever secret manager the command names — the same resolution path
/// <c>SecretManagerSigningCredentialProvider</c> already uses for the JWT signing key — never
/// configuration, never a file beside the application.
/// </summary>
/// <remarks>
/// <para>
/// Storage format is <c>nonce (12 bytes) || ciphertext || tag (16 bytes)</c>, one concatenated blob so
/// a caller has exactly one thing to persist and one thing to read back. The nonce is generated fresh
/// per call — an AES-GCM nonce must never repeat under the same key, and letting the caller supply one
/// would just move that invariant somewhere it can be gotten wrong.
/// </para>
/// <para>
/// Resolved keys are cached per (secret manager, key name) for <see cref="_cacheLifetime"/>, the same
/// way <c>SecretManagerSigningCredentialProvider</c> caches the JWT signing key. A secret manager is
/// not always cheap to call — <c>Fdw.Services.SecretManagers.AzureKeyVault</c> is a network round-trip
/// — and this executor sits in the hot path of every flow suspend and resume, not a once-per-startup
/// read. A rotation still takes effect once the cache entry expires, without a restart.
/// </para>
/// </remarks>
public sealed class AesGcmEncryptionCommandExecutor : IEncryptionCommandExecutor, IDisposable
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private const int KeySizeBytes = 32;

    private readonly ISecretManagerProvider _secrets;
    private readonly TimeSpan _cacheLifetime;
    private readonly ILogger<AesGcmEncryptionCommandExecutor> _logger;
    private readonly ConcurrentDictionary<string, CachedKey> _cache = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="AesGcmEncryptionCommandExecutor"/> class.</summary>
    /// <param name="secrets">Resolves the configured secret manager.</param>
    /// <param name="cacheLifetime">How long a resolved key is reused before it is fetched again.</param>
    /// <param name="logger">The logger.</param>
    public AesGcmEncryptionCommandExecutor(
        ISecretManagerProvider secrets,
        TimeSpan cacheLifetime,
        ILogger<AesGcmEncryptionCommandExecutor>? logger = null)
    {
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _cacheLifetime = cacheLifetime;
        _logger = logger ?? NullLogger<AesGcmEncryptionCommandExecutor>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<byte[]>> Execute(
        EncryptCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        var key = await ResolveKey(command.SecretManagerName, command.SecretKeyName, cancellationToken)
            .ConfigureAwait(false);
        if (key.IsFailure)
            return key.ToNewResult<byte[]>();

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[command.Plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using (var aes = new AesGcm(key.Value!, TagSizeBytes))
        {
            aes.Encrypt(nonce, command.Plaintext, ciphertext, tag);
        }

        var blob = new byte[NonceSizeBytes + ciphertext.Length + TagSizeBytes];
        Buffer.BlockCopy(nonce, 0, blob, 0, NonceSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, blob, NonceSizeBytes, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, blob, NonceSizeBytes + ciphertext.Length, TagSizeBytes);

        EncryptionLog.Encrypted(_logger, command.SecretManagerName, command.SecretKeyName);
        return GenericResult<byte[]>.Success(blob);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<byte[]>> Execute(
        DecryptCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        if (command.Ciphertext.Length < NonceSizeBytes + TagSizeBytes)
            return GenericResult<byte[]>.Failure(EncryptionLog.CiphertextTooShort(_logger));

        var key = await ResolveKey(command.SecretManagerName, command.SecretKeyName, cancellationToken)
            .ConfigureAwait(false);
        if (key.IsFailure)
            return key.ToNewResult<byte[]>();

        var nonce = new byte[NonceSizeBytes];
        var tag = new byte[TagSizeBytes];
        var ciphertextLength = command.Ciphertext.Length - NonceSizeBytes - TagSizeBytes;
        var ciphertext = new byte[ciphertextLength];
        var plaintext = new byte[ciphertextLength];

        Buffer.BlockCopy(command.Ciphertext, 0, nonce, 0, NonceSizeBytes);
        Buffer.BlockCopy(command.Ciphertext, NonceSizeBytes, ciphertext, 0, ciphertextLength);
        Buffer.BlockCopy(command.Ciphertext, NonceSizeBytes + ciphertextLength, tag, 0, TagSizeBytes);

        try
        {
            using var aes = new AesGcm(key.Value!, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException ex)
        {
            // Carried rather than swallowed: a tag mismatch means tampering or the wrong key, and
            // neither leaks any material — the exception's own message names neither plaintext nor key.
            return GenericResult<byte[]>.Failure(
                EncryptionLog.DecryptionFailed(_logger, ex, command.SecretKeyName));
        }

        EncryptionLog.Decrypted(_logger, command.SecretManagerName, command.SecretKeyName);
        return GenericResult<byte[]>.Success(plaintext);
    }

    private async Task<IGenericResult<byte[]>> ResolveKey(
        string secretManagerName, string keyName, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKey(secretManagerName, keyName);
        if (_cache.TryGetValue(cacheKey, out var hit) && hit.LoadedAt.Add(_cacheLifetime) > DateTimeOffset.UtcNow)
        {
            EncryptionLog.KeyReused(_logger, secretManagerName, keyName);
            return GenericResult<byte[]>.Success(hit.Key);
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Why checked again: another caller may have loaded the key while this one waited.
            if (_cache.TryGetValue(cacheKey, out hit) && hit.LoadedAt.Add(_cacheLifetime) > DateTimeOffset.UtcNow)
                return GenericResult<byte[]>.Success(hit.Key);

            var manager = await _secrets.Get(secretManagerName, cancellationToken).ConfigureAwait(false);
            if (manager.IsFailure || manager.Value is null)
                return GenericResult<byte[]>.Failure(EncryptionLog.SecretManagerUnresolved(_logger, secretManagerName));

            var secret = await manager.Value
                .Execute<SecretValue>(new GetSecretManagerCommand(container: null, secretKey: keyName), cancellationToken)
                .ConfigureAwait(false);
            if (secret.IsFailure || secret.Value is null)
                return GenericResult<byte[]>.Failure(EncryptionLog.KeyUnreadable(_logger, keyName));

            byte[] key;
            try
            {
                key = secret.Value.AccessStringValue(Convert.FromBase64String);
            }
            catch (FormatException ex)
            {
                return GenericResult<byte[]>.Failure(EncryptionLog.KeyNotBase64(_logger, ex, keyName));
            }

            if (key.Length != KeySizeBytes)
                return GenericResult<byte[]>.Failure(EncryptionLog.KeyMalformed(_logger, keyName));

            _cache[cacheKey] = new CachedKey(key, DateTimeOffset.UtcNow);
            EncryptionLog.KeyLoaded(_logger, secretManagerName, keyName);
            return GenericResult<byte[]>.Success(key);
        }
        finally
        {
            _gate.Release();
        }
    }

    // Why a separator no secret manager or key name can contain: ("a", "b/c") and ("a/b", "c") must
    // never share an entry.
    private static string CacheKey(string secretManagerName, string keyName)
        => string.Concat(secretManagerName, "\u0000", keyName);

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}
