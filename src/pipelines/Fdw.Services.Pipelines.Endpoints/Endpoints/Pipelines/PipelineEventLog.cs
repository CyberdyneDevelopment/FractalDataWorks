using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// MessageLogging definitions for the endpoint that receives pipeline events relayed from a host
/// that serves no hub (FDW-3773). EventId range: 20810-20811, 11310; the batch ones (FDW-5249): 20820-20821, 11315.
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS9")]
public static partial class PipelineEventLog
{
    /// <summary>Logs a relayed event that names no known kind.</summary>
    [MessageLogging(EventId = 20810, Level = LogLevel.Warning, Message = "Relayed pipeline event names unknown kind '{kind}'", ClientMessage = "'{kind}' is not a pipeline event kind")]
    public static partial IGenericMessage UnknownKind(ILogger logger, string kind);

    /// <summary>Logs a relayed event that lacks the body its kind needs.</summary>
    [MessageLogging(EventId = 20811, Level = LogLevel.Warning, Message = "Relayed pipeline event of kind '{kind}' lacks the body that kind needs", ClientMessage = "An event of kind '{kind}' needs a body")]
    public static partial IGenericMessage BodyMissing(ILogger logger, string kind);

    /// <summary>Logs a relayed event that was broadcast.</summary>
    [MessageLogging(EventId = 11310, Level = LogLevel.Information, Message = "Relayed pipeline event of kind '{kind}' broadcast")]
    public static partial IGenericMessage Relayed(ILogger logger, string kind);

    /// <summary>Logs an event of a relayed batch that names no known kind.</summary>
    [MessageLogging(EventId = 20820, Level = LogLevel.Warning, Message = "Event {index} of a relayed pipeline event batch names unknown kind '{kind}'", ClientMessage = "Event {index} of the batch: '{kind}' is not a pipeline event kind")]
    public static partial IGenericMessage BatchEventUnknownKind(ILogger logger, int index, string kind);

    /// <summary>Logs an event of a relayed batch that lacks the body its kind needs.</summary>
    [MessageLogging(EventId = 20821, Level = LogLevel.Warning, Message = "Event {index} of a relayed pipeline event batch, of kind '{kind}', lacks the body that kind needs", ClientMessage = "Event {index} of the batch, of kind '{kind}', needs a body")]
    public static partial IGenericMessage BatchEventBodyMissing(ILogger logger, int index, string kind);

    /// <summary>Logs a relayed batch whose events were all broadcast.</summary>
    [MessageLogging(EventId = 11315, Level = LogLevel.Information, Message = "Relayed pipeline event batch of {count} events broadcast")]
    public static partial IGenericMessage BatchRelayed(ILogger logger, int count);
}
