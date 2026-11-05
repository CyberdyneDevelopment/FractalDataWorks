using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Logging;

/// <summary>
/// MessageLogging for the routing of pipeline events between the SignalR broadcaster and the relay (FDW-3773).
/// EventId range: 11320, 60310-60312, 71310-71311.
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
}
