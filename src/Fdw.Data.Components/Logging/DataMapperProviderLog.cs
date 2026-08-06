using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging for DataMapperProvider operations.
/// EventId range: 1800-1809
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class DataMapperProviderLog
{
    /// <summary>
    /// Logs that loading the available connections failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading connections.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71008,
        Level = LogLevel.Error,
        Message = "Failed to load connections")]
    public static partial IGenericMessage LoadConnectionsFailed(
        ILogger logger,
        Exception exception);

    /// <summary>
    /// Logs that loading the tables for the named connection failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading tables.</param>
    /// <param name="connection">The name of the connection whose tables failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71009,
        Level = LogLevel.Error,
        Message = "Failed to load tables for connection '{connection}'")]
    public static partial IGenericMessage LoadTablesFailed(
        ILogger logger,
        Exception exception,
        string connection);

    /// <summary>
    /// Logs that a number of fields were automatically mapped.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of fields that were auto-mapped.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Auto-mapped {count} fields")]
    public static partial IGenericMessage AutoMappedFields(
        ILogger logger,
        int count);

    /// <summary>
    /// Logs that field mappings were prepared for the specified DataSet and source.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of field mappings that were prepared.</param>
    /// <param name="dataSet">The name of the DataSet the mappings were prepared for.</param>
    /// <param name="source">The name of the source the mappings were prepared for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "Prepared {count} field mappings for '{dataSet}/{source}'")]
    public static partial IGenericMessage MappingsPrepared(
        ILogger logger,
        int count,
        string dataSet,
        string source);

    /// <summary>
    /// Logs that an error occurred while preparing field mappings.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while preparing field mappings.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 91006,
        Level = LogLevel.Error,
        Message = "Error preparing field mappings")]
    public static partial IGenericMessage MappingsPreparationFailed(
        ILogger logger,
        Exception exception);

    /// <summary>
    /// Logs that loading the named DataStore for the mapper failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the DataStore.</param>
    /// <param name="dataStore">The name of the DataStore that failed to load.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71010,
        Level = LogLevel.Error,
        Message = "Failed to load DataStore '{dataStore}' for mapper")]
    public static partial IGenericMessage LoadDataStoreFailed(
        ILogger logger,
        Exception exception,
        string dataStore);

    /// <summary>
    /// Logs that the named DataStore was loaded, with the number of paths it contains.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataStore">The name of the DataStore that was loaded.</param>
    /// <param name="pathCount">The number of paths in the loaded DataStore.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Trace,
        Message = "Loaded DataStore '{dataStore}': {pathCount} paths")]
    public static partial IGenericMessage DataStoreLoaded(
        ILogger logger,
        string dataStore,
        int pathCount);

    /// <summary>
    /// Logs that field mappings were saved for the specified DataSet and source.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of field mappings that were saved.</param>
    /// <param name="dataSet">The name of the DataSet the mappings were saved for.</param>
    /// <param name="source">The name of the source the mappings were saved for.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Information,
        Message = "Saved {count} field mappings for DataSet '{dataSet}' source '{source}'")]
    public static partial IGenericMessage MappingsSaved(
        ILogger logger,
        int count,
        string dataSet,
        string source);

    /// <summary>
    /// Logs that saving the field mappings for the specified DataSet and source failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSet">The name of the DataSet whose mappings failed to save.</param>
    /// <param name="source">The name of the source whose mappings failed to save.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71011,
        Level = LogLevel.Error,
        Message = "Failed to save field mappings for DataSet '{dataSet}' source '{source}'")]
    public static partial IGenericMessage MappingsSaveFailed(
        ILogger logger,
        string dataSet,
        string source);

    /// <summary>
    /// Logs that loading the DataStore list for the mapper failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="exception">The exception that was thrown while loading the DataStore list.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 71012,
        Level = LogLevel.Error,
        Message = "Failed to load DataStore list for mapper")]
    public static partial IGenericMessage LoadDataStoresFailed(
        ILogger logger,
        Exception exception);

    /// <summary>
    /// Logs that a number of DataStores were loaded for the mapper.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="count">The number of DataStores that were loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11012,
        Level = LogLevel.Information,
        Message = "Loaded {count} DataStores for mapper")]
    public static partial IGenericMessage DataStoresLoaded(
        ILogger logger,
        int count);
}
