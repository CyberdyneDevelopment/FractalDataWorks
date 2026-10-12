using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Security.Encryption.Logging;

/// <summary>
/// MessageLogging for <see cref="AesGcmEncryptionCommandExecutor"/>.
/// </summary>
/// <remarks>
/// EventId range: 7000–7009. Neither plaintext, ciphertext, nor key material ever appears here at
/// any level — a log is the wrong place for any of the three, and a key name is the only thing about
/// a key that is ever safe to record.
/// </remarks>
[MessageLoggingTypeCode("ENCRYPT")]
internal static partial class EncryptionLog
{
    /// <summary>Bytes were encrypted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The secret manager the key came from.</param>
    /// <param name="keyName">The key's name.</param>
    [MessageLogging(EventId = 7000, Level = LogLevel.Trace,
        Message = "Encrypted using key '{keyName}' from secret manager '{secretManagerName}'")]
    internal static partial IGenericMessage Encrypted(
        ILogger<AesGcmEncryptionCommandExecutor> logger, string secretManagerName, string keyName);

    /// <summary>Bytes were decrypted.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The secret manager the key came from.</param>
    /// <param name="keyName">The key's name.</param>
    [MessageLogging(EventId = 7001, Level = LogLevel.Trace,
        Message = "Decrypted using key '{keyName}' from secret manager '{secretManagerName}'")]
    internal static partial IGenericMessage Decrypted(
        ILogger<AesGcmEncryptionCommandExecutor> logger, string secretManagerName, string keyName);

    /// <summary>The named secret manager could not be resolved.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The secret manager that was requested.</param>
    [MessageLogging(EventId = 7002, Level = LogLevel.Error,
        Message = "Secret manager '{secretManagerName}' could not be resolved")]
    internal static partial IGenericMessage SecretManagerUnresolved(
        ILogger<AesGcmEncryptionCommandExecutor> logger, string secretManagerName);

    /// <summary>The named key could not be read from its secret manager.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyName">The key that was requested.</param>
    [MessageLogging(EventId = 7003, Level = LogLevel.Error,
        Message = "Key '{keyName}' could not be read from its secret manager")]
    internal static partial IGenericMessage KeyUnreadable(
        ILogger<AesGcmEncryptionCommandExecutor> logger, string keyName);

    /// <summary>The resolved key material is not a valid AES-256 key.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="keyName">The key that was requested.</param>
    [MessageLogging(EventId = 7004, Level = LogLevel.Critical,
        Message = "Key '{keyName}' is not valid base64-encoded 256-bit key material")]
    internal static partial IGenericMessage KeyMalformed(
        ILogger<AesGcmEncryptionCommandExecutor> logger, string keyName);

    /// <summary>A key's stored value is not base64.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The decoding exception. Its message never carries the input, so no key material is logged.</param>
    /// <param name="keyName">The key that was requested.</param>
    [MessageLogging(EventId = 7009, Level = LogLevel.Critical,
        Message = "Key '{keyName}' is not base64 and cannot be used as key material")]
    internal static partial IGenericMessage KeyNotBase64(
        ILogger<AesGcmEncryptionCommandExecutor> logger, Exception ex, string keyName);

    /// <summary>Decryption failed integrity verification.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The underlying cryptographic exception.</param>
    /// <param name="keyName">The key that was used.</param>
    [MessageLogging(EventId = 7005, Level = LogLevel.Error,
        Message = "Decryption with key '{keyName}' failed integrity verification")]
    internal static partial IGenericMessage DecryptionFailed(
        ILogger<AesGcmEncryptionCommandExecutor> logger, Exception ex, string keyName);

    /// <summary>Ciphertext was too short to contain a nonce and tag.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 7006, Level = LogLevel.Error,
        Message = "Ciphertext is too short to contain a nonce and authentication tag")]
    internal static partial IGenericMessage CiphertextTooShort(
        ILogger<AesGcmEncryptionCommandExecutor> logger);

    /// <summary>A key was read from its secret manager and cached.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The secret manager the key came from.</param>
    /// <param name="keyName">The key's name.</param>
    [MessageLogging(EventId = 7007, Level = LogLevel.Debug,
        Message = "Loaded key '{keyName}' from secret manager '{secretManagerName}'")]
    internal static partial IGenericMessage KeyLoaded(
        ILogger<AesGcmEncryptionCommandExecutor> logger, string secretManagerName, string keyName);

    /// <summary>A cached key was reused without calling its secret manager.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="secretManagerName">The secret manager the key came from.</param>
    /// <param name="keyName">The key's name.</param>
    [MessageLogging(EventId = 7008, Level = LogLevel.Trace,
        Message = "Reused cached key '{keyName}' from secret manager '{secretManagerName}'")]
    internal static partial IGenericMessage KeyReused(
        ILogger<AesGcmEncryptionCommandExecutor> logger, string secretManagerName, string keyName);
}
