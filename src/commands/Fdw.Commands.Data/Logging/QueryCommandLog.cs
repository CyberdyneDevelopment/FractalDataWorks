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

    /// <summary>Logs that a union names a container or column that is not a plain name (FDW-6946): refused, never written into the statement.</summary>
    [MessageLogging(EventId = 22033, Level = LogLevel.Error,
        Message = "Query on container '{containerName}' stacks a union that names '{identifier}', which is not a plain name",
        ClientMessage = "The query on '{containerName}' stacks a union that names '{identifier}', which is not a plain name")]
    public static partial IGenericMessage UnionIdentifierInvalid(ILogger logger, string containerName, string identifier);

    /// <summary>
    /// Logs that the branches of a union do not give the same columns in the same order (FDW-6946): every branch of a union gives every column of
    /// the first, in its order, so a row of one is a row of the others.
    /// </summary>
    [MessageLogging(EventId = 22034, Level = LogLevel.Error,
        Message = "Query on container '{containerName}' stacks branch '{branch}' with columns ({columns}), which are not the first branch's ({expected})",
        ClientMessage = "The query on '{containerName}' stacks branch '{branch}' with columns ({columns}), which are not the first branch's ({expected})")]
    public static partial IGenericMessage UnionBranchesDoNotFit(ILogger logger, string containerName, string branch, string columns, string expected);

    /// <summary>Logs that a union stacks a container its store does not declare exactly once (FDW-6946).</summary>
    [MessageLogging(EventId = 22035, Level = LogLevel.Error,
        Message = "Query on container '{containerName}' stacks container '{branch}', which its data store declares {count} times; it must be declared once",
        ClientMessage = "The query on '{containerName}' stacks container '{branch}', which its data store declares {count} times; it must be declared once")]
    public static partial IGenericMessage UnionContainerNotDeclaredOnce(ILogger logger, string containerName, string branch, int count);

    /// <summary>
    /// Logs that a query pages a union and states no order (FDW-6946): stacked rows have no one declared order to page by, so the order is the
    /// caller's to state and none is guessed.
    /// </summary>
    [MessageLogging(EventId = 22036, Level = LogLevel.Error,
        Message = "Query on container '{containerName}' pages a union and states no order to page by",
        ClientMessage = "The query on '{containerName}' pages a union and states no order to page by")]
    public static partial IGenericMessage UnionPagedWithoutOrder(ILogger logger, string containerName);

    /// <summary>Logs that a query states a union together with joins or a grouping (FDW-6946): a union stacks rows and does neither.</summary>
    [MessageLogging(EventId = 22037, Level = LogLevel.Error,
        Message = "Query on container '{containerName}' states a union with {joinCount} joins and grouping {grouped}; a union stacks rows and does neither",
        ClientMessage = "The query on '{containerName}' states a union with {joinCount} joins and grouping {grouped}; a union stacks rows and does neither")]
    public static partial IGenericMessage UnionWithJoinsOrGrouping(ILogger logger, string containerName, int joinCount, bool grouped);
}
