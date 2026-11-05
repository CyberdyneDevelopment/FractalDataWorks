using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.PostgreSql.Logging;

/// <summary>
/// Message logging for PostgreSQL data command translators.
/// </summary>
[MessageLoggingTypeCode("PGSQL")]
public static partial class PostgreSqlTranslatorLog
{
    /// <summary>
    /// Logs when a translator receives an invalid command type.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Translator '{translatorName}' expected {expectedType} but received {actualType}",
        TypeCode = new[] { 'P', 'G', 'S' })]
    public static partial IGenericMessage InvalidCommandType(
        ILogger logger,
        string translatorName,
        string expectedType,
        string actualType);

    /// <summary>
    /// Logs a query that pages its rows with no ordering when its container declares no primary key to order by (FDW-3454).
    /// </summary>
    [MessageLogging(
        EventId = 21020,
        Level = LogLevel.Error,
        Message = "Query on container '{containerName}' pages its rows with no ordering, and the container declares no primary key to order them by; state an ordering",
        ClientMessage = "The query on '{containerName}' pages its rows with no ordering, and '{containerName}' declares no primary key to order them by; state an ordering",
        TypeCode = new[] { 'P', 'G', 'S' })]
    public static partial IGenericMessage PagedQueryHasNoOrder(
        ILogger logger,
        string containerName);
}
