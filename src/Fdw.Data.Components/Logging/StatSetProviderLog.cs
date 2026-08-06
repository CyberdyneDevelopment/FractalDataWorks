using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging for StatSetProvider operations.
/// EventId range: 4234-4239
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class StatSetProviderLog
{
    /// <summary>
    /// Logs that statistics are being computed for the specified number of columns.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="columnCount">The number of columns statistics are being computed for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11079,
        Level = LogLevel.Trace,
        Message = "Computing statistics for {columnCount} columns")]
    public static partial IGenericMessage ComputingStats(ILogger logger, int columnCount);

    /// <summary>
    /// Logs that statistics have been computed for the specified number of columns.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="columnCount">The number of columns statistics were computed for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11080,
        Level = LogLevel.Information,
        Message = "Statistics computed for {columnCount} columns")]
    public static partial IGenericMessage StatsComputed(ILogger logger, int columnCount);

    /// <summary>
    /// Logs that statistics computation failed.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="exception">The exception that caused the statistics computation to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91043,
        Level = LogLevel.Warning,
        Message = "Statistics computation failed")]
    public static partial IGenericMessage StatsComputationFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that a column was selected for detail.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="columnName">The name of the column selected for detail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11081,
        Level = LogLevel.Trace,
        Message = "Column selected for detail: '{columnName}'")]
    public static partial IGenericMessage ColumnSelected(ILogger logger, string columnName);
}
