using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging for VisualizationProvider operations.
/// EventId range: 11082-11087, 91044-91056 (DATACOMPONENTS TypeCode).
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class VisualizationProviderLog
{
    /// <summary>
    /// Logs that the visualization provider was initialized with the specified visualization type.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="visualizationType">The visualization type the provider was initialized with.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11082,
        Level = LogLevel.Trace,
        Message = "VisualizationProvider initialized with type '{visualizationType}'")]
    public static partial IGenericMessage Initialized(ILogger logger, string visualizationType);

    /// <summary>
    /// Logs that the visualization type was changed to the specified value.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="visualizationType">The visualization type the provider was changed to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11083,
        Level = LogLevel.Information,
        Message = "Visualization type changed to '{visualizationType}'")]
    public static partial IGenericMessage VisualizationTypeChanged(ILogger logger, string visualizationType);

    /// <summary>
    /// Logs that a StatSet is being computed for the visualization.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11084,
        Level = LogLevel.Trace,
        Message = "Computing StatSet for visualization")]
    public static partial IGenericMessage ComputingStatSet(ILogger logger);

    /// <summary>
    /// Logs that the StatSet was computed with the specified number of columns.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="columnCount">The number of columns in the computed StatSet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11085,
        Level = LogLevel.Information,
        Message = "StatSet computed with {columnCount} columns")]
    public static partial IGenericMessage StatSetComputed(ILogger logger, int columnCount);

    /// <summary>
    /// Logs that the StatSet computation failed.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="exception">The exception that caused the StatSet computation to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91044,
        Level = LogLevel.Warning,
        Message = "StatSet computation failed")]
    public static partial IGenericMessage StatSetComputationFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that filters were applied with the specified number of conditions.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="filterCount">The number of filter conditions applied.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11086,
        Level = LogLevel.Information,
        Message = "Filters applied: {filterCount} conditions")]
    public static partial IGenericMessage FiltersApplied(ILogger logger, int filterCount);

    /// <summary>
    /// Logs that a calculation was added for the specified operation on the given source column.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="operation">The calculation operation that was added.</param>
    /// <param name="sourceColumn">The source column the calculation operates on.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11087,
        Level = LogLevel.Information,
        Message = "Calculation added: {operation} on '{sourceColumn}'")]
    public static partial IGenericMessage CalculationAdded(ILogger logger, string operation, string sourceColumn);

    // ═══════════════════════════════════════════════════════════════════════════
    // VisualizePageProvider (91045-91055)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs that the dataset list is being loaded for the Visualize picker.</summary>
    [MessageLogging(EventId = 91045, Level = LogLevel.Trace, Message = "VisualizePageProvider: loading dataset list")]
    public static partial IGenericMessage LoadingDataSets(ILogger logger);

    /// <summary>Logs that the dataset list loaded successfully.</summary>
    [MessageLogging(EventId = 91046, Level = LogLevel.Information,
        Message = "VisualizePageProvider: loaded {dataSetCount} datasets")]
    public static partial IGenericMessage DataSetsLoaded(ILogger logger, int dataSetCount);

    /// <summary>Logs that loading the dataset list failed (API non-success).</summary>
    [MessageLogging(EventId = 91047, Level = LogLevel.Error,
        Message = "VisualizePageProvider: failed to load dataset list: {reason}")]
    public static partial IGenericMessage LoadDataSetsFailed(ILogger logger, string reason);

    /// <summary>Logs that loading the dataset list failed with an exception.</summary>
    [MessageLogging(EventId = 91053, Level = LogLevel.Error,
        Message = "VisualizePageProvider: failed to load dataset list")]
    public static partial IGenericMessage LoadDataSetsException(ILogger logger, Exception exception);

    /// <summary>Logs that a dataset is being previewed for chart rendering.</summary>
    [MessageLogging(EventId = 91048, Level = LogLevel.Trace,
        Message = "VisualizePageProvider: previewing dataset '{dataSetName}'")]
    public static partial IGenericMessage PreviewingDataSet(ILogger logger, string dataSetName);

    /// <summary>Logs that a dataset preview loaded successfully.</summary>
    [MessageLogging(EventId = 91049, Level = LogLevel.Information,
        Message = "VisualizePageProvider: preview loaded {rowCount} rows, {columnCount} columns for '{dataSetName}'")]
    public static partial IGenericMessage PreviewLoaded(ILogger logger, string dataSetName, int rowCount, int columnCount);

    /// <summary>Logs that a dataset preview failed (API non-success or empty result).</summary>
    [MessageLogging(EventId = 91050, Level = LogLevel.Error,
        Message = "VisualizePageProvider: failed to preview dataset '{dataSetName}': {reason}")]
    public static partial IGenericMessage PreviewFailed(ILogger logger, string dataSetName, string reason);

    /// <summary>Logs that a dataset preview failed with an exception.</summary>
    [MessageLogging(EventId = 91054, Level = LogLevel.Error,
        Message = "VisualizePageProvider: failed to preview dataset '{dataSetName}'")]
    public static partial IGenericMessage PreviewException(ILogger logger, string dataSetName, Exception exception);

    /// <summary>Logs that the chart type was changed by the user.</summary>
    [MessageLogging(EventId = 91051, Level = LogLevel.Trace,
        Message = "VisualizePageProvider: chart type changed to '{chartTypeName}'")]
    public static partial IGenericMessage ChartTypeSelected(ILogger logger, string chartTypeName);

    /// <summary>Logs that an encoding role was bound to a column.</summary>
    [MessageLogging(EventId = 91052, Level = LogLevel.Trace,
        Message = "VisualizePageProvider: role '{roleName}' bound to column '{columnName}'")]
    public static partial IGenericMessage EncodingBound(ILogger logger, string roleName, string columnName);

    /// <summary>
    /// Logs that loading the dataset list failed and the result carried no message.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91055, Level = LogLevel.Error,
        Message = "VisualizePageProvider: failed to load dataset list (the API result carried no message)")]
    public static partial IGenericMessage LoadDataSetsFailedNoMessage(ILogger logger);

    /// <summary>
    /// Logs that previewing a dataset failed and the result carried no message.
    /// </summary>
    /// <param name="logger">The logger to write the log event to.</param>
    /// <param name="dataSetName">The dataset whose preview failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 91056, Level = LogLevel.Error,
        Message = "VisualizePageProvider: failed to preview dataset '{dataSetName}' (the API result carried no message)")]
    public static partial IGenericMessage PreviewFailedNoMessage(ILogger logger, string dataSetName);
}
