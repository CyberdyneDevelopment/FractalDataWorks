using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.SecretManagers.Logging;

/// <summary>
/// Message logging for <see cref="Fdw.Services.SecretManagers.Commands.ListSecretsManagerCommand"/>.
/// </summary>
[MessageLoggingTypeCode("SECRETMGR")]
public static partial class ListSecretsManagerCommandLog
{
    [MessageLogging(
        EventId = 12003,
        Level = LogLevel.Trace,
        Message = "ListSecretsManagerCommand constructed for container '{container}'")]
    public static partial IGenericMessage Constructed(
        ILogger logger,
        string? container);
}
