using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Logging;

/// <summary>
/// MessageLogging for DataSetConfigurationProvider operations.
/// EventId range: 9351, 9380-9399
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataSetConfigurationProviderLog
{
    /// <summary>
    /// Logs that the DataSet child hierarchy was loaded, reporting the number of sources.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetName">The name of the DataSet whose child hierarchy was loaded.</param>
    /// <param name="sourceCount">The number of sources loaded for the DataSet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11051, Level = LogLevel.Debug,
        Message = "Loading DataSet child hierarchy for '{dataSetName}': {sourceCount} sources")]
    public static partial IGenericMessage ChildHierarchyLoaded(ILogger logger, string dataSetName, int sourceCount);

    /// <summary>
    /// Logs that field mappings were loaded for a DataSet source.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sourceName">The name of the DataSet source whose field mappings were loaded.</param>
    /// <param name="mappingCount">The number of field mappings loaded for the source.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11052, Level = LogLevel.Trace,
        Message = "Loading field mappings for DataSet source '{sourceName}': {mappingCount} mappings")]
    public static partial IGenericMessage FieldMappingsLoaded(ILogger logger, string sourceName, int mappingCount);

    /// <summary>
    /// Logs that loading the child hierarchy for a DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetName">The name of the DataSet whose child hierarchy failed to load.</param>
    /// <param name="reason">The reason the child hierarchy load failed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71011, Level = LogLevel.Warning,
        Message = "Failed to load child hierarchy for DataSet '{dataSetName}': {reason}")]
    public static partial IGenericMessage ChildHierarchyLoadFailed(ILogger logger, string dataSetName, string reason);

    /// <summary>
    /// Logs that a DataSet was assembled, reporting the number of sources and total field mappings.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetName">The name of the DataSet that was assembled.</param>
    /// <param name="sourceCount">The number of sources in the assembled DataSet.</param>
    /// <param name="totalMappings">The total number of field mappings in the assembled DataSet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11053, Level = LogLevel.Debug,
        Message = "DataSet '{dataSetName}' assembled with {sourceCount} sources, {totalMappings} total field mappings")]
    public static partial IGenericMessage HierarchyAssembled(ILogger logger, string dataSetName, int sourceCount, int totalMappings);

    /// <summary>
    /// Logs that all DataSet child caches (sources and field mappings) are being loaded.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11054, Level = LogLevel.Trace,
        Message = "Loading all DataSet child caches: sources, field mappings")]
    public static partial IGenericMessage LoadingAllChildCaches(ILogger logger);

    /// <summary>
    /// Logs that all DataSet child caches were loaded, reporting the source and field mapping counts.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sourceCount">The number of sources loaded into the child caches.</param>
    /// <param name="mappingCount">The number of field mappings loaded into the child caches.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11055, Level = LogLevel.Debug,
        Message = "All DataSet child caches loaded: {sourceCount} sources, {mappingCount} field mappings")]
    public static partial IGenericMessage AllChildCachesLoaded(ILogger logger, int sourceCount, int mappingCount);

    // Fields read/write — 9386-9393

    /// <summary>
    /// Logs that fields are being read for a DataSet.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose fields are being read.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11056, Level = LogLevel.Trace,
        Message = "Reading fields for DataSet {dataSetId}")]
    public static partial IGenericMessage GetFieldsTrace(ILogger logger, System.Guid dataSetId);

    /// <summary>
    /// Logs that fields were read for a DataSet, reporting the field count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose fields were read.</param>
    /// <param name="count">The number of fields read for the DataSet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11057, Level = LogLevel.Information,
        Message = "Fields read for DataSet {dataSetId}: {count} fields")]
    public static partial IGenericMessage GetFieldsLoaded(ILogger logger, System.Guid dataSetId, int count);

    /// <summary>
    /// Logs that reading fields for a DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose fields failed to read.</param>
    /// <param name="ex">The exception that caused the field read to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71012, Level = LogLevel.Error,
        Message = "Failed to read fields for DataSet {dataSetId}")]
    public static partial IGenericMessage GetFieldsFailed(ILogger logger, System.Guid dataSetId, System.Exception ex);

    /// <summary>
    /// Logs that fields are being saved for a DataSet, reporting the field count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose fields are being saved.</param>
    /// <param name="count">The number of fields being saved for the DataSet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11058, Level = LogLevel.Trace,
        Message = "Saving fields for DataSet {dataSetId}: {count} fields")]
    public static partial IGenericMessage SaveFieldsTrace(ILogger logger, System.Guid dataSetId, int count);

    /// <summary>
    /// Logs that fields were saved for a DataSet.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose fields were saved.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11059, Level = LogLevel.Information,
        Message = "Fields saved for DataSet {dataSetId}")]
    public static partial IGenericMessage SaveFieldsSaved(ILogger logger, System.Guid dataSetId);

    /// <summary>
    /// Logs that retiring field rows for a DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose field rows failed to retire.</param>
    /// <param name="ex">The exception that caused the field-row retirement to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71013, Level = LogLevel.Error,
        Message = "Failed to retire field rows for DataSet {dataSetId}")]
    public static partial IGenericMessage RetireFieldsFailed(ILogger logger, System.Guid dataSetId, System.Exception ex);

    /// <summary>
    /// Logs that inserting field rows for a DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose field rows failed to insert.</param>
    /// <param name="ex">The exception that caused the field-row insert to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71014, Level = LogLevel.Error,
        Message = "Failed to insert field rows for DataSet {dataSetId}")]
    public static partial IGenericMessage InsertFieldsFailed(ILogger logger, System.Guid dataSetId, System.Exception ex);

    // Sources, KeyFields save — 9393-9399

    /// <summary>
    /// Logs that sources are being saved for a DataSet, reporting the source count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose sources are being saved.</param>
    /// <param name="count">The number of sources being saved for the DataSet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11060, Level = LogLevel.Trace,
        Message = "Saving {count} source(s) for DataSet {dataSetId}")]
    public static partial IGenericMessage SaveSourcesTrace(ILogger logger, System.Guid dataSetId, int count);

    /// <summary>
    /// Logs that sources were saved for a DataSet, reporting the record count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose sources were saved.</param>
    /// <param name="count">The number of source records saved for the DataSet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11061, Level = LogLevel.Information,
        Message = "Sources saved for DataSet {dataSetId}: {count} record(s)")]
    public static partial IGenericMessage SaveSourcesSaved(ILogger logger, System.Guid dataSetId, int count);

    /// <summary>
    /// Logs that retiring source rows for a DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose source rows failed to retire.</param>
    /// <param name="ex">The exception that caused the source-row retirement to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71015, Level = LogLevel.Error,
        Message = "Failed to retire source rows for DataSet {dataSetId}")]
    public static partial IGenericMessage RetireSourcesFailed(ILogger logger, System.Guid dataSetId, System.Exception ex);

    /// <summary>
    /// Logs that inserting source rows for a DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose source rows failed to insert.</param>
    /// <param name="ex">The exception that caused the source-row insert to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71016, Level = LogLevel.Error,
        Message = "Failed to insert source rows for DataSet {dataSetId}")]
    public static partial IGenericMessage InsertSourcesFailed(ILogger logger, System.Guid dataSetId, System.Exception ex);

    /// <summary>
    /// Logs that key fields are being saved for a DataSet, reporting the key field count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose key fields are being saved.</param>
    /// <param name="count">The number of key fields being saved for the DataSet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11062, Level = LogLevel.Trace,
        Message = "Saving {count} key field(s) for DataSet {dataSetId}")]
    public static partial IGenericMessage SaveKeyFieldsTrace(ILogger logger, System.Guid dataSetId, int count);

    /// <summary>
    /// Logs that key fields were saved for a DataSet, reporting the record count.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose key fields were saved.</param>
    /// <param name="count">The number of key field records saved for the DataSet.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    // Why: 9351 used (from the available 9351-9379 band) because 9393-9399 were fully consumed
    // before the need for a SaveKeyFieldsSaved Information log was identified.
    [MessageLogging(EventId = 11063, Level = LogLevel.Information,
        Message = "Key fields saved for DataSet {dataSetId}: {count} record(s)")]
    public static partial IGenericMessage SaveKeyFieldsSaved(ILogger logger, System.Guid dataSetId, int count);

    /// <summary>
    /// Logs that retiring key field rows for a DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose key field rows failed to retire.</param>
    /// <param name="ex">The exception that caused the key-field-row retirement to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71017, Level = LogLevel.Error,
        Message = "Failed to retire key field rows for DataSet {dataSetId}")]
    public static partial IGenericMessage RetireKeyFieldsFailed(ILogger logger, System.Guid dataSetId, System.Exception ex);

    /// <summary>
    /// Logs that inserting key field rows for a DataSet failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="dataSetId">The identifier of the DataSet whose key field rows failed to insert.</param>
    /// <param name="ex">The exception that caused the key-field-row insert to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71018, Level = LogLevel.Error,
        Message = "Failed to insert key field rows for DataSet {dataSetId}")]
    public static partial IGenericMessage InsertKeyFieldsFailed(ILogger logger, System.Guid dataSetId, System.Exception ex);

    /// <summary>
    /// Logs that querying field mappings for a DataSet source failed.
    /// </summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="sourceName">The name of the source whose field mappings failed to query.</param>
    /// <param name="dataSetName">The name of the DataSet that owns the source.</param>
    /// <param name="ex">The exception that caused the field mapping query to fail.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 71019, Level = LogLevel.Error,
        Message = "Failed to query field mappings for source '{sourceName}' of DataSet '{dataSetName}'")]
    public static partial IGenericMessage FieldMappingQueryFailed(ILogger logger, string sourceName, string dataSetName, System.Exception ex);
}
