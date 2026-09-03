using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretProtection.Logging;

/// <summary>
/// MessageLogging for <see cref="SecretProtectorTypes"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETPROTECTION")]
internal static partial class SecretProtectionLog
{
    /// <summary>No implementation package installed a registration before Register ran.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 93000, Level = LogLevel.Critical,
        Message = "No ISecretProtector implementation is registered -- reference an implementation package (e.g. Fdw.Services.SecretProtection.DataProtection)")]
    internal static partial IGenericMessage NoImplementationRegistered(ILogger logger);

    /// <summary>The installed implementation's registration delegate threw.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception that occurred.</param>
    [MessageLogging(EventId = 93001, Level = LogLevel.Critical,
        Message = "The registered ISecretProtector implementation failed to register")]
    internal static partial IGenericMessage ImplementationRegistrationFailed(ILogger logger, Exception exception);
}
