using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// Static logger class for StatSet service operations using MessageLogging infrastructure.
/// EventId range: 9300-9320
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class StatSetServiceLog
{
    /// <summary>
    /// Logs when computing statistics for columns.
    /// </summary>
    [MessageLogging(
        EventId = 11260,
        Level = LogLevel.Debug,
        Message = "Computing statistics for {columnCount} columns on '{containerName}' via connection '{connectionName}'")]
    public static partial IGenericMessage ComputingStatSet(
        ILogger logger,
        int columnCount,
        string containerName,
        string connectionName);

    /// <summary>
    /// Logs when statistics computation completes successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11261,
        Level = LogLevel.Debug,
        Message = "Statistics computed for '{containerName}' in {durationMs}ms")]
    public static partial IGenericMessage StatSetComputed(
        ILogger logger,
        string containerName,
        double durationMs);

    /// <summary>
    /// Logs when statistics computation fails.
    /// </summary>
    [MessageLogging(
        EventId = 91024,
        Level = LogLevel.Error,
        Message = "Statistics computation failed for '{containerName}': {error}")]
    public static partial IGenericMessage StatSetComputationFailed(
        ILogger logger,
        string containerName,
        string? error);

    /// <summary>
    /// Logs when computing grouped statistics.
    /// </summary>
    [MessageLogging(
        EventId = 11262,
        Level = LogLevel.Debug,
        Message = "Computing grouped statistics for {columnCount} columns grouped by {groupByCount} columns on '{containerName}'")]
    public static partial IGenericMessage ComputingGroupedStatSet(
        ILogger logger,
        int columnCount,
        int groupByCount,
        string containerName);

    /// <summary>
    /// Logs when grouped statistics computation completes.
    /// </summary>
    [MessageLogging(
        EventId = 11263,
        Level = LogLevel.Debug,
        Message = "Grouped statistics computed for '{containerName}' with {groupCount} groups in {durationMs}ms")]
    public static partial IGenericMessage GroupedStatSetComputed(
        ILogger logger,
        string containerName,
        int groupCount,
        double durationMs);

    /// <summary>
    /// Logs when no column names are specified in the request.
    /// </summary>
    [MessageLogging(
        EventId = 21009,
        Level = LogLevel.Warning,
        Message = "No column names specified in StatSet request for '{containerName}'")]
    public static partial IGenericMessage NoColumnsSpecified(
        ILogger logger,
        string containerName);

    /// <summary>
    /// Logs when retrieving data for statistics computation.
    /// </summary>
    [MessageLogging(
        EventId = 11264,
        Level = LogLevel.Trace,
        Message = "Retrieving data for statistics computation from '{containerName}'")]
    public static partial IGenericMessage RetrievingDataForStats(
        ILogger logger,
        string containerName);

    /// <summary>
    /// Logs when a query succeeds but returns no data.
    /// </summary>
    // Why Error, not Warning (FDW-583): the call site returns GenericResult.Failure for this
    // condition — the operation could not complete, so the log severity must match the outcome.
    [MessageLogging(
        EventId = 31041,
        Level = LogLevel.Error,
        Message = "Statistics query returned no data for '{containerName}'")]
    public static partial IGenericMessage StatSetQueryReturnedNoData(
        ILogger logger,
        string containerName);
}
