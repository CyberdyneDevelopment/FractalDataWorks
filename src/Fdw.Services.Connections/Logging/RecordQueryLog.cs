using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for the shared, format-agnostic record-query evaluator
/// (<see cref="RowQuery.RecordQueryEvaluator"/>, <see cref="RowQuery.RecordQueryValidator"/>,
/// <see cref="RowQuery.RecordRowMaterializer"/>) that any record-connector-based connection
/// (FileSystem, Http) reuses to apply a <c>QueryCommand</c>'s filter/join over decoded rows and
/// materialize the matched rows to a POCO type.
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class RecordQueryLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Trace (12300-12301)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the evaluator is about to apply a filter/join over the supplied rows.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="rowCount">The number of primary rows to evaluate.</param>
    /// <param name="hasFilter">Whether the command carries a filter expression.</param>
    /// <param name="joinCount">The number of joins the command carries.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 12300, Level = LogLevel.Trace, Message = "Evaluating record query over {rowCount} row(s) (hasFilter={hasFilter}, joinCount={joinCount})")]
    public static partial IGenericMessage EvaluatingQuery(ILogger logger, int rowCount, bool hasFilter, int joinCount);

    /// <summary>
    /// Logs that the evaluator finished applying the filter/join.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="matchedCount">The number of rows that matched the filter/join.</param>
    /// <param name="rowCount">The total number of primary rows considered.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 12301, Level = LogLevel.Information, Message = "Record query matched {matchedCount} of {rowCount} row(s)")]
    public static partial IGenericMessage QueryEvaluated(ILogger logger, int matchedCount, int rowCount);

    /// <summary>
    /// Logs that every decoded row was checked against the container's declared field schema.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="containerName">The container whose declared fields the rows were checked against.</param>
    /// <param name="rowCount">The number of rows validated.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 12302, Level = LogLevel.Trace, Message = "Validated {rowCount} decoded row(s) against container '{containerName}' declared field schema")]
    public static partial IGenericMessage RowsValidated(ILogger logger, string containerName, int rowCount);

    /// <summary>
    /// Logs that the join target container was resolved and its rows loaded.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="containerName">The joined (parent) container name.</param>
    /// <param name="parentRowCount">The number of parent rows loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 12303, Level = LogLevel.Trace, Message = "Join target container '{containerName}' resolved with {parentRowCount} row(s)")]
    public static partial IGenericMessage JoinTargetResolved(ILogger logger, string containerName, int parentRowCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // Missing (31004) — a declared JOIN target that the container tree cannot resolve
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the command's JOIN target container is not a sibling of the primary container.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="containerName">The joined container name the command requested.</param>
    /// <param name="pathName">The path (schema) the primary container belongs to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 31004, Level = LogLevel.Error, Message = "Join target container '{containerName}' was not found in path '{pathName}'")]
    public static partial IGenericMessage JoinedContainerNotFound(ILogger logger, string containerName, string pathName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Validation (21010-21018) — the AND-of-equality, single-INNER-join grammar,
    // plus the declared-schema checks (filter/join columns, required row fields).
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that the command carries more than one join — only a single INNER join is supported.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="joinCount">The number of joins the command carried.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21010, Level = LogLevel.Error, Message = "Unsupported query shape: {joinCount} join(s) requested — at most one INNER join is supported")]
    public static partial IGenericMessage UnsupportedJoinCount(ILogger logger, int joinCount);

    /// <summary>
    /// Logs that the command's single join is not a single-condition INNER join.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="joinType">The join type the command requested.</param>
    /// <param name="conditionCount">The number of join conditions the command requested.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21011, Level = LogLevel.Error, Message = "Unsupported query shape: join type '{joinType}' with {conditionCount} condition(s) — only a single-condition INNER join is supported")]
    public static partial IGenericMessage UnsupportedJoinShape(ILogger logger, string joinType, int conditionCount);

    /// <summary>
    /// Logs that a filter condition uses an operator other than equality, or a filter group uses OR.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="propertyName">The property name the unsupported condition/group applies to.</param>
    /// <param name="sqlOperator">The unsupported operator's SQL representation.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21012, Level = LogLevel.Error, Message = "Unsupported query shape: operator '{sqlOperator}' on '{propertyName}' — only AND-of-equality filters are supported")]
    public static partial IGenericMessage UnsupportedFilterOperator(ILogger logger, string propertyName, string sqlOperator);

    /// <summary>
    /// Logs that the filter tree carries a node type the in-memory evaluator has no grammar for.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="nodeType">The CLR type name of the unsupported filter node.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21013, Level = LogLevel.Error, Message = "Unsupported query shape: filter node type '{nodeType}' — only FilterCondition leaves and FilterGroup composites are supported")]
    public static partial IGenericMessage UnsupportedFilterNodeType(ILogger logger, string nodeType);

    /// <summary>
    /// Logs that a decoded row omits (or nulls) a field the container declares as non-nullable.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="containerName">The container whose declared schema the row violates.</param>
    /// <param name="fieldName">The declared non-nullable field that is absent or null.</param>
    /// <param name="rowIndex">The zero-based index of the offending row.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21014, Level = LogLevel.Error, Message = "Container '{containerName}' declares field '{fieldName}' as non-nullable, but row {rowIndex} omits it or carries null")]
    public static partial IGenericMessage RequiredFieldMissingInRow(ILogger logger, string containerName, string fieldName, int rowIndex);

    /// <summary>
    /// Logs that a filter references a column the target container does not declare as a field.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="columnName">The column the filter references.</param>
    /// <param name="containerName">The container the column was resolved against.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21015, Level = LogLevel.Error, Message = "Filter references column '{columnName}', which container '{containerName}' does not declare as a field")]
    public static partial IGenericMessage FilterColumnNotDeclared(ILogger logger, string columnName, string containerName);

    /// <summary>
    /// Logs that a join condition references a column the target container does not declare as a field.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="columnName">The column the join condition references.</param>
    /// <param name="containerName">The container the column was resolved against.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21016, Level = LogLevel.Error, Message = "Join condition references column '{columnName}', which container '{containerName}' does not declare as a field")]
    public static partial IGenericMessage JoinColumnNotDeclared(ILogger logger, string columnName, string containerName);

    /// <summary>
    /// Logs that a compared value carries a CLR type the equality evaluator has no defined semantics for.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="typeName">The CLR type name of the unsupported value.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21017, Level = LogLevel.Error, Message = "Unsupported comparison value type '{typeName}' — only string, bool, Guid, numeric, DateTime and DateTimeOffset values can be compared")]
    public static partial IGenericMessage UnsupportedComparisonValueType(ILogger logger, string typeName);

    /// <summary>
    /// Logs that a filter condition carries a table qualifier that names neither the primary container
    /// nor the joined container.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="qualifier">The unrecognised qualifier.</param>
    /// <param name="propertyName">The fully-qualified property name the filter carried.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 21018, Level = LogLevel.Error, Message = "Filter qualifier '{qualifier}' on '{propertyName}' names neither the queried container nor the joined container")]
    public static partial IGenericMessage UnknownFilterQualifier(ILogger logger, string qualifier, string propertyName);

    // ═══════════════════════════════════════════════════════════════════════════
    // Internal (91010-91011) — materialization wiring defects
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logs that no generated <c>PocoMapper</c> is registered for the requested materialization type.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="typeName">The type name for which no mapper was found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error, Message = "No PocoMapper registered for type '{typeName}'")]
    public static partial IGenericMessage NoMapperFound(ILogger logger, string typeName);

    /// <summary>
    /// Logs that a matched row failed to materialize into the requested type.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="typeName">The type name the row failed to materialize into.</param>
    /// <param name="reason">The underlying mapping failure reason.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91011, Level = LogLevel.Error, Message = "Failed to materialize a row into '{typeName}': {reason}")]
    public static partial IGenericMessage MaterializationFailed(ILogger logger, string typeName, string? reason);
}
