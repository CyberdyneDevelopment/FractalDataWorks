using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Logging;

/// <summary>
/// MessageLogging for the routing of pipeline events between the SignalR broadcaster and the relay (FDW-3773).
/// EventId range: 11320, 11324, 60310-60312, 60314-60315, 71310-71311, 71315-71316 (the batch ones are FDW-5249).
/// </summary>
[MessageLoggingTypeCode("PIPELINES")]
public static partial class PipelineEventRelayLog
{
    /// <summary>Logs a broadcast dropped because this host's Host row could not be read.</summary>
    [MessageLogging(EventId = 60310, Level = LogLevel.Error, Message = "Pipeline event dropped: no usable Host row named '{hostName}', so whether this host serves the hub is not known")]
    public static partial IGenericMessage HostRowUnavailable(ILogger logger, string hostName);

    /// <summary>Logs a broadcast dropped because this host serves no hub and has no relay client.</summary>
    [MessageLogging(EventId = 60311, Level = LogLevel.Error, Message = "Pipeline event dropped: host '{hostName}' serves no hub and registers no PipelineEventClient to relay to one")]
    public static partial IGenericMessage RelayClientMissing(ILogger logger, string hostName);

    /// <summary>Logs a relay the hub host refused or could not be reached for.</summary>
    [MessageLogging(EventId = 71310, Level = LogLevel.Error, Message = "Pipeline event of kind '{kind}' was not delivered to the hub host")]
    public static partial IGenericMessage RelayFailed(ILogger logger, string kind);

    /// <summary>Logs a relay that threw before or while sending.</summary>
    [MessageLogging(EventId = 71311, Level = LogLevel.Error, Message = "Pipeline event of kind '{kind}' threw while being sent to the hub host")]
    public static partial IGenericMessage RelayException(ILogger logger, System.Exception exception, string kind);

    /// <summary>Logs, once, that this host publishes no pipeline events because the relay is switched off.</summary>
    [MessageLogging(EventId = 11320, Level = LogLevel.Information, Message = "Pipeline event relay is switched off by setting '{setting}': this host publishes no pipeline events to the hub host")]
    public static partial IGenericMessage RelayOff(ILogger logger, string setting);

    /// <summary>Logs, once, that the relay setting has no value, so this host publishes nothing.</summary>
    [MessageLogging(EventId = 60312, Level = LogLevel.Error, Message = "Pipeline event relay setting '{setting}' has no value: this host publishes no pipeline events to the hub host until it is set")]
    public static partial IGenericMessage RelaySettingMissing(ILogger logger, string setting);

    /// <summary>Logs a components update dropped because the relay buffer reached its capacity.</summary>
    [MessageLogging(EventId = 60314, Level = LogLevel.Warning, Message = "Pipeline event relay buffer is at its capacity of {capacity} events: the oldest components update, of execution {executionId}, was dropped")]
    public static partial IGenericMessage RelayBufferFull(ILogger logger, int capacity, System.Guid executionId);

    /// <summary>Logs that the pipeline hub declares no positive whole number for a relay tuning key, so the relay cannot start.</summary>
    [MessageLogging(EventId = 60315, Level = LogLevel.Error, Message = "The pipeline hub declares no positive whole number '{key}' (found '{declared}') and no host overrides it, so the pipeline event relay does not start")]
    public static partial IGenericMessage RelayTuningNotDeclared(ILogger logger, string key, string declared);

    /// <summary>Logs a batch of events the hub host refused or could not be reached for.</summary>
    [MessageLogging(EventId = 71315, Level = LogLevel.Error, Message = "A batch of {count} pipeline events was not delivered to the hub host")]
    public static partial IGenericMessage BatchRelayFailed(ILogger logger, int count);

    /// <summary>Logs a batch that threw before or while sending.</summary>
    [MessageLogging(EventId = 71316, Level = LogLevel.Error, Message = "A batch of {count} pipeline events threw while being sent to the hub host")]
    public static partial IGenericMessage BatchRelayException(ILogger logger, System.Exception exception, int count);

    /// <summary>Logs that the relay flusher started, with the tuning it reads.</summary>
    [MessageLogging(EventId = 11324, Level = LogLevel.Information, Message = "Pipeline event relay sends the events of each {flushMilliseconds} ms in one request, from a buffer of at most {capacity} events")]
    public static partial IGenericMessage RelayFlusherStarted(ILogger logger, int flushMilliseconds, int capacity);
}
