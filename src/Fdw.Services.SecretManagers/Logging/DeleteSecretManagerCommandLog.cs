using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Services.SecretManagers.Commands.DeleteSecretManagerCommand"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class DeleteSecretManagerCommandLog
{
    [MessageLogging(
        EventId = 12001,
        Level = LogLevel.Trace,
        Message = "DeleteSecretManagerCommand constructed for container '{container}' secret '{secretKey}'")]
    public static partial IGenericMessage Constructed(
        ILogger logger,
        string? container,
        string secretKey);

    // Why: reuses the FDW canonical RequiredValueMissing number (20000) — see
    // SecretManagerCommandBaseLog.RequiredValueMissing's remark.
    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Critical,
        Message = "DeleteSecretManagerCommand: required value '{parameterName}' is missing")]
    public static partial IGenericMessage RequiredValueMissing(
        ILogger logger,
        string parameterName);
}
