using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.UI.Components.Blazor.Logging;

/// <summary>
/// MessageLogging methods for ConnectionSelector component operations.
/// EventId range: 7000-7009
/// </summary>
[MessageLoggingTypeCode("COMPONENTS8")]
public static partial class ConnectionSelectorLog
{
    /// <summary>Logs when loading the connections list fails.</summary>
    [MessageLogging(EventId = 71019, Level = LogLevel.Warning,
        Message = "ConnectionSelector: Failed to load connections")]
    public static partial IGenericMessage LoadConnectionsFailed(
        ILogger logger);

    /// <summary>Logs when loading the connections list fails with exception.</summary>
    [MessageLogging(EventId = 71020, Level = LogLevel.Warning,
        Message = "ConnectionSelector: Failed to load connections")]
    public static partial IGenericMessage LoadConnectionsException(
        ILogger logger,
        Exception exception);
}
