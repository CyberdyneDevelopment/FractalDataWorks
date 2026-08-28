using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Services.SecretManagers.Commands.SetSecretManagerCommand"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class SetSecretManagerCommandLog
{
    [MessageLogging(
        EventId = 12002,
        Level = LogLevel.Trace,
        Message = "SetSecretManagerCommand constructed for container '{container}' secret '{secretKey}'")]
    public static partial IGenericMessage Constructed(
        ILogger logger,
        string? container,
        string secretKey);

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Critical,
        Message = "SetSecretManagerCommand: required value '{parameterName}' is missing")]
    public static partial IGenericMessage RequiredValueMissing(
        ILogger logger,
        string parameterName);
}
