using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>MessageLogging for the connection-type endpoints.</summary>
/// <remarks>EventIds follow the result categories: 1 progress, 3 missing (404).</remarks>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class ConnectionTypeEndpointLog
{
    /// <summary>Logs how many default-value policies a connection type registers.</summary>
    [MessageLogging(EventId = 11550, Level = LogLevel.Debug, Message = "Connection type '{connectionType}' registers {count} default-value policies")]
    public static partial IGenericMessage ListedDefaultValuePolicies(ILogger logger, string connectionType, int count);

    /// <summary>Logs that no connection type has the given name.</summary>
    [MessageLogging(EventId = 31550, Level = LogLevel.Warning, Message = "No connection type named '{connectionType}'; see /connections/types")]
    public static partial IGenericMessage ConnectionTypeUnknown(ILogger logger, string connectionType);

    /// <summary>Logs that the route carried no connection type.</summary>
    [MessageLogging(EventId = 31551, Level = LogLevel.Warning, Message = "No connection type in the route")]
    public static partial IGenericMessage ConnectionTypeRouteValueMissing(ILogger logger);
}
