using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for the fields a connection's cursor reads (FDW-6970). EventId range: 22820-22829.
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class CursorFieldsLog
{
    /// <summary>Logs that a query that joins or stacks containers states no projection, so its columns are not known to the cursor.</summary>
    [MessageLogging(EventId = 22820, Level = LogLevel.Warning,
        Message = "A query sent to container '{container}' joins or stacks containers and states no projection, so the cursor does not know the columns it returns")]
    public static partial IGenericMessage CombinedColumnsNotStated(ILogger logger, string container);

    /// <summary>Logs that a column of a query that joins or stacks containers states no alias, the name the cursor reads it by.</summary>
    [MessageLogging(EventId = 22821, Level = LogLevel.Warning,
        Message = "A query sent to container '{container}' joins or stacks containers and returns column '{column}' without an alias, the name the cursor reads it by")]
    public static partial IGenericMessage CombinedColumnWithoutAlias(ILogger logger, string container, string column);
}
