using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>The coded messages the connections endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("CONNANSWERS")]
public static partial class ConnectionsAnswerLog
{

    /// <summary>Logs that Connection schema was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Connection schema was not found, or is not visible to the caller", ClientMessage = "Connection schema was not found")]
    public static partial IGenericMessage GetConnectionSchemaConnectionschemaNotFound(ILogger logger);

    /// <summary>Logs that the request failed: Schema discovery failed.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "Schema discovery failed", ClientMessage = "Schema discovery failed")]
    public static partial IGenericMessage RefreshConnectionSchemaSchemaDiscoveryFailed(ILogger logger);
}
