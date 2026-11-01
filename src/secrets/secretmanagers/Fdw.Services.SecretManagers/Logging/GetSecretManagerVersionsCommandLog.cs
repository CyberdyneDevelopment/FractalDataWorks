using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Services.SecretManagers.Commands.GetSecretManagerVersionsCommand"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class GetSecretManagerVersionsCommandLog
{
    [MessageLogging(
        EventId = 12005,
        Level = LogLevel.Trace,
        Message = "GetSecretManagerVersionsCommand constructed for container '{container}' secret '{secretKey}'")]
    public static partial IGenericMessage Constructed(
        ILogger logger,
        string? container,
        string secretKey);
}
