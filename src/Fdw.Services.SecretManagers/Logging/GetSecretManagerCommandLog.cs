using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Services.SecretManagers.Commands.GetSecretManagerCommand"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class GetSecretManagerCommandLog
{
    [MessageLogging(
        EventId = 12000,
        Level = LogLevel.Trace,
        Message = "GetSecretManagerCommand constructed for container '{container}' secret '{secretKey}'")]
    public static partial IGenericMessage Constructed(
        ILogger logger,
        string? container,
        string secretKey);

    [MessageLogging(
        EventId = 20000,
        Level = LogLevel.Critical,
        Message = "GetSecretManagerCommand: required value '{parameterName}' is missing")]
    public static partial IGenericMessage RequiredValueMissing(
        ILogger logger,
        string parameterName);
}
