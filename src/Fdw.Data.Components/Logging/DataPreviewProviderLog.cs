using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging for DataPreviewProvider operations.
/// EventId range: 1810-1824
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class DataPreviewProviderLog
{
    /// <summary>
    /// Logs that the DataStore list is being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11013,
        Level = LogLevel.Trace,
        Message = "Loading DataStore list")]
    public static partial IGenericMessage LoadingDataStores(ILogger logger);

    /// <summary>
    /// Logs that a number of DataStores were loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of DataStores that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11014,
        Level = LogLevel.Information,
        Message = "Loaded {count} DataStores")]
    public static partial IGenericMessage LoadedDataStores(ILogger logger, int count);

    /// <summary>
    /// Logs that loading the DataStore list failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the DataStore list.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71013,
        Level = LogLevel.Error,
        Message = "Failed to load DataStore list")]
    public static partial IGenericMessage LoadDataStoresFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that the detail for the named DataStore is being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStoreName">The name of the DataStore whose detail is being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11015,
        Level = LogLevel.Trace,
        Message = "Loading DataStore detail for '{dataStoreName}'")]
    public static partial IGenericMessage LoadingDataStoreDetail(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs that the detail for the named DataStore was loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStoreName">The name of the DataStore whose detail was loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11016,
        Level = LogLevel.Information,
        Message = "Loaded DataStore detail for '{dataStoreName}'")]
    public static partial IGenericMessage LoadedDataStoreDetail(ILogger logger, string dataStoreName);

    /// <summary>
    /// Logs that loading the detail for the named DataStore failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the DataStore detail.</param>
    /// <param name="dataStoreName">The name of the DataStore whose detail failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71014,
        Level = LogLevel.Error,
        Message = "Failed to load DataStore detail for '{dataStoreName}'")]
    public static partial IGenericMessage LoadDataStoreDetailFailed(ILogger logger, Exception exception, string dataStoreName);

    /// <summary>
    /// Logs that the DataSet list is being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11017,
        Level = LogLevel.Trace,
        Message = "Loading DataSet list")]
    public static partial IGenericMessage LoadingDataSets(ILogger logger);

    /// <summary>
    /// Logs that a number of DataSets were loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of DataSets that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11018,
        Level = LogLevel.Information,
        Message = "Loaded {count} DataSets")]
    public static partial IGenericMessage LoadedDataSets(ILogger logger, int count);

    /// <summary>
    /// Logs that loading the DataSet list failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the DataSet list.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71015,
        Level = LogLevel.Error,
        Message = "Failed to load DataSet list")]
    public static partial IGenericMessage LoadDataSetsFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that the detail for the named DataSet is being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetName">The name of the DataSet whose detail is being loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11019,
        Level = LogLevel.Trace,
        Message = "Loading DataSet detail for '{dataSetName}'")]
    public static partial IGenericMessage LoadingDataSetDetail(ILogger logger, string dataSetName);

    /// <summary>
    /// Logs that the detail for the named DataSet was loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetName">The name of the DataSet whose detail was loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11020,
        Level = LogLevel.Information,
        Message = "Loaded DataSet detail for '{dataSetName}'")]
    public static partial IGenericMessage LoadedDataSetDetail(ILogger logger, string dataSetName);

    /// <summary>
    /// Logs that loading the detail for the named DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the DataSet detail.</param>
    /// <param name="dataSetName">The name of the DataSet whose detail failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71016,
        Level = LogLevel.Error,
        Message = "Failed to load DataSet detail for '{dataSetName}'")]
    public static partial IGenericMessage LoadDataSetDetailFailed(ILogger logger, Exception exception, string dataSetName);

    /// <summary>
    /// Logs that the preview mode changed to the specified mode.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="mode">The mode the preview changed to.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11021,
        Level = LogLevel.Trace,
        Message = "Mode changed to {mode}")]
    public static partial IGenericMessage ModeChanged(ILogger logger, string mode);

    /// <summary>
    /// Logs that a container was selected, identified by its path and container name.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="path">The path of the selected container.</param>
    /// <param name="container">The name of the selected container.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11022,
        Level = LogLevel.Trace,
        Message = "Container selected: path='{path}' container='{container}'")]
    public static partial IGenericMessage ContainerSelected(ILogger logger, string path, string container);

    /// <summary>
    /// Logs that a table was selected, identified by its schema and table name.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="schema">The schema of the selected table.</param>
    /// <param name="table">The name of the selected table.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11023,
        Level = LogLevel.Trace,
        Message = "Table selected: schema='{schema}' table='{table}'")]
    public static partial IGenericMessage TableSelected(ILogger logger, string schema, string table);

    /// <summary>
    /// Logs that a DataSet preview pane is loading preview data.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="datasetName">The name of the DataSet being previewed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11024,
        Level = LogLevel.Trace,
        Message = "Loading preview for DataSet '{datasetName}'")]
    public static partial IGenericMessage LoadingPreviewPane(ILogger logger, string datasetName);

    /// <summary>
    /// Logs that loading the DataSet preview pane failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="datasetName">The name of the DataSet whose preview failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71017,
        Level = LogLevel.Error,
        Message = "Failed to load preview for DataSet '{datasetName}'")]
    public static partial IGenericMessage LoadPreviewPaneFailed(ILogger logger, Exception exception, string datasetName);

    /// <summary>
    /// Logs that the DataSet preview pane was opened.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="datasetName">The name of the DataSet whose pane was opened.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11025,
        Level = LogLevel.Trace,
        Message = "Preview pane opened for '{datasetName}'")]
    public static partial IGenericMessage TogglePaneOpen(ILogger logger, string datasetName);

    /// <summary>
    /// Logs that the DataSet preview pane was closed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="datasetName">The name of the DataSet whose pane was closed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11026,
        Level = LogLevel.Trace,
        Message = "Preview pane closed for '{datasetName}'")]
    public static partial IGenericMessage TogglePaneClose(ILogger logger, string datasetName);

    /// <summary>
    /// Logs that the DataSet preview row limit was updated.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="limit">The new row limit value.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11027,
        Level = LogLevel.Trace,
        Message = "Row limit updated to {limit}")]
    public static partial IGenericMessage UpdateRowLimit(ILogger logger, int limit);
}
