using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging for FilterPanelProvider operations.
/// EventId range: 4240-4245
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class FilterPanelProviderLog
{
    /// <summary>
    /// Logs that a filter condition was added for the specified column and operator.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="columnName">The name of the column the filter condition targets.</param>
    /// <param name="operatorName">The name of the operator used by the filter condition.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11075,
        Level = LogLevel.Trace,
        Message = "Filter condition added: column='{columnName}' operator='{operatorName}'")]
    public static partial IGenericMessage ConditionAdded(ILogger logger, string columnName, string operatorName);

    /// <summary>
    /// Logs that a filter condition was removed at the given index.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="index">The index of the filter condition that was removed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11076,
        Level = LogLevel.Trace,
        Message = "Filter condition removed at index {index}")]
    public static partial IGenericMessage ConditionRemoved(ILogger logger, int index);

    /// <summary>
    /// Logs that the filters were applied, with the number of conditions applied.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="conditionCount">The number of filter conditions that were applied.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11077,
        Level = LogLevel.Information,
        Message = "Filters applied: {conditionCount} conditions")]
    public static partial IGenericMessage FiltersApplied(ILogger logger, int conditionCount);

    /// <summary>
    /// Logs that all filter conditions were cleared.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11078,
        Level = LogLevel.Information,
        Message = "All filter conditions cleared")]
    public static partial IGenericMessage FiltersCleared(ILogger logger);
}
