using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Commands.Data.Logging;

/// <summary>
/// MessageLogging for <see cref="QueryCommand{T}"/> construction.
/// </summary>
[MessageLoggingTypeCode("CMDDATA")]
public static partial class QueryCommandLog
{
    /// <summary>Traces a query command being constructed.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "[QueryCommand] Created for entity type '{entityType}'")]
    public static partial IGenericMessage CommandCreated(ILogger logger, string entityType);

    /// <summary>Logs that a query joins by a name that is no join type (FDW-5099): refused, never written into the statement.</summary>
    [MessageLogging(EventId = 22030, Level = LogLevel.Error,
        Message = "Query on container '{containerName}' joins by '{joinType}', which is not a join type; use one of: {joinTypes}",
        ClientMessage = "The query on '{containerName}' joins by '{joinType}', which is not a join type; use one of: {joinTypes}")]
    public static partial IGenericMessage JoinTypeNotRecognised(ILogger logger, string containerName, string joinType, string joinTypes);

    /// <summary>Logs that a query joins by a join type the store's queries do not perform (FDW-5099).</summary>
    [MessageLogging(EventId = 22031, Level = LogLevel.Error,
        Message = "Query on container '{containerName}' joins by '{joinType}', which this store's queries do not perform; use one of: {joinTypes}",
        ClientMessage = "The query on '{containerName}' joins by '{joinType}', which this store's queries do not perform; use one of: {joinTypes}")]
    public static partial IGenericMessage JoinTypeNotPerformed(ILogger logger, string containerName, string joinType, string joinTypes);

    /// <summary>
    /// Logs that a query's join conditions do not fit its join type (FDW-5099): a join on a condition has none, or a join of every row with
    /// every row has some.
    /// </summary>
    [MessageLogging(EventId = 22032, Level = LogLevel.Error,
        Message = "Query on container '{containerName}' joins by '{joinType}' with {conditionCount} join conditions; a join on a condition needs at least one, and a join of every row with every row takes none",
        ClientMessage = "The query on '{containerName}' joins by '{joinType}' with {conditionCount} join conditions; a join on a condition needs at least one, and a join of every row with every row takes none")]
    public static partial IGenericMessage JoinConditionsDoNotFit(ILogger logger, string containerName, string joinType, int conditionCount);
}
