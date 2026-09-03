using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretProtection.DataProtection.Logging;

/// <summary>
/// MessageLogging for <see cref="DataProtectionSecretProtector"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETPROTECTION")]
internal static partial class DataProtectionSecretProtectorLog
{
    /// <summary>Protect failed -- the key ring cannot encrypt with its current key.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception that occurred.</param>
    [MessageLogging(EventId = 93010, Level = LogLevel.Error, Message = "Failed to protect data")]
    internal static partial IGenericMessage ProtectFailed(ILogger logger, Exception exception);

    /// <summary>Unprotect failed -- the ciphertext does not match any key in this key ring.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception that occurred.</param>
    [MessageLogging(EventId = 93011, Level = LogLevel.Error, Message = "Failed to unprotect data")]
    internal static partial IGenericMessage UnprotectFailed(ILogger logger, Exception exception);
}
