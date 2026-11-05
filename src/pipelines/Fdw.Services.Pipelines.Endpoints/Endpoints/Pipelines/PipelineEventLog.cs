using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// MessageLogging definitions for the endpoint that receives pipeline events relayed from a host
/// that serves no hub (FDW-3773). EventId range: 20810-20811, 11310.
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS9")]
public static partial class PipelineEventLog
{
    /// <summary>Logs a relayed event that names no known kind.</summary>
    [MessageLogging(EventId = 20810, Level = LogLevel.Warning, Message = "Relayed pipeline event names unknown kind '{kind}'")]
    public static partial IGenericMessage UnknownKind(ILogger logger, string kind);

    /// <summary>Logs a relayed event that lacks the body its kind needs.</summary>
    [MessageLogging(EventId = 20811, Level = LogLevel.Warning, Message = "Relayed pipeline event of kind '{kind}' lacks the body that kind needs")]
    public static partial IGenericMessage BodyMissing(ILogger logger, string kind);

    /// <summary>Logs a relayed event that was broadcast.</summary>
    [MessageLogging(EventId = 11310, Level = LogLevel.Information, Message = "Relayed pipeline event of kind '{kind}' broadcast")]
    public static partial IGenericMessage Relayed(ILogger logger, string kind);
}
