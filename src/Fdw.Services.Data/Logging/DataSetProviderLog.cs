using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// Source-generated logging methods for DataSetProvider.
/// </summary>
[MessageLoggingTypeCode("DATA")]
public static partial class DataSetProviderLog
{
    /// <summary>Logs retrieval of a DataSet from registry.</summary>
    [MessageLogging(EventId = 11066, Level = LogLevel.Debug, Message = "Retrieved DataSet '{dataSetName}' from registry")]
    public static partial IGenericMessage DataSetRetrieved(ILogger logger, string dataSetName);

    /// <summary>Logs when a DataSet is not found in registry.</summary>
    [MessageLogging(EventId = 31011, Level = LogLevel.Warning, Message = "DataSet '{dataSetName}' not found in registry")]
    public static partial IGenericMessage DataSetNotFound(ILogger logger, string dataSetName);

    /// <summary>Logs retrieval of all DataSets from registry.</summary>
    [MessageLogging(EventId = 11067, Level = LogLevel.Information, Message = "Retrieved {count} DataSets from registry")]
    public static partial IGenericMessage AllDataSetsRetrieved(ILogger logger, int count);

    /// <summary>Logs registration of a DataSet.</summary>
    [MessageLogging(EventId = 11068, Level = LogLevel.Information, Message = "Registered DataSet '{dataSetName}' with {sourceCount} sources")]
    public static partial IGenericMessage DataSetRegistered(ILogger logger, string dataSetName, int sourceCount);

    /// <summary>Logs unregistration of a DataSet.</summary>
    [MessageLogging(EventId = 11069, Level = LogLevel.Information, Message = "Unregistered DataSet '{dataSetName}'")]
    public static partial IGenericMessage DataSetUnregistered(ILogger logger, string dataSetName);

    /// <summary>Logs selection of default source for a DataSet.</summary>
    [MessageLogging(EventId = 11070, Level = LogLevel.Information, Message = "Selected default source '{sourceName}' for DataSet '{dataSetName}' (Priority: {priority})")]
    public static partial IGenericMessage DefaultSourceSelected(
        ILogger logger,
        string dataSetName,
        string sourceName,
        int priority);

    /// <summary>Logs retrieval of a source from a DataSet.</summary>
    [MessageLogging(EventId = 11071, Level = LogLevel.Debug, Message = "Retrieved source '{sourceName}' from DataSet '{dataSetName}'")]
    public static partial IGenericMessage SourceRetrieved(ILogger logger, string dataSetName, string sourceName);

    /// <summary>Logs when a source is not found in a DataSet.</summary>
    [MessageLogging(EventId = 31012, Level = LogLevel.Warning, Message = "Source '{sourceName}' not found in DataSet '{dataSetName}'")]
    public static partial IGenericMessage SourceNotFound(ILogger logger, string dataSetName, string sourceName);

    /// <summary>Logs retrieval of field mappings for a source.</summary>
    [MessageLogging(EventId = 11072, Level = LogLevel.Debug, Message = "Retrieved {count} field mappings for source '{sourceName}' in DataSet '{dataSetName}'")]
    public static partial IGenericMessage FieldMappingsRetrieved(
        ILogger logger,
        string dataSetName,
        string sourceName,
        int count);

    /// <summary>Logs successful resolution of a record type.</summary>
    [MessageLogging(EventId = 11073, Level = LogLevel.Debug, Message = "Resolved record type '{typeName}' for DataSet '{dataSetName}'")]
    public static partial IGenericMessage RecordTypeResolved(ILogger logger, string dataSetName, string typeName);

    /// <summary>Logs failure to resolve a record type.</summary>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error, Message = "Failed to resolve record type '{recordTypeName}' for DataSet '{dataSetName}': {error}")]
    public static partial IGenericMessage RecordTypeResolutionFailed(
        ILogger logger,
        string dataSetName,
        string recordTypeName,
        string error);

    // ============================================================
    // Dual-Source Provider Logging (EventId 5120-5129)
    // ============================================================

    /// <summary>Logs when the configured index is rebuilt.</summary>
    [MessageLogging(EventId = 11074, Level = LogLevel.Information, Message = "DataSet index rebuilt: {dataSetCount} datasets, {sourceCount} sources")]
    public static partial IGenericMessage DataSetIndexRebuilt(ILogger logger, int dataSetCount, int sourceCount);

    /// <summary>Logs when a DataSet is retrieved by ID.</summary>
    [MessageLogging(EventId = 11075, Level = LogLevel.Debug, Message = "DataSet retrieved by ID '{id}' from {source}")]
    public static partial IGenericMessage DataSetRetrievedById(ILogger logger, Guid id, string source);

    /// <summary>Logs when a Source is retrieved by ID.</summary>
    [MessageLogging(EventId = 11076, Level = LogLevel.Debug, Message = "Source retrieved by ID '{sourceId}'")]
    public static partial IGenericMessage SourceRetrievedById(ILogger logger, Guid sourceId);

    /// <summary>Logs when a DataSet with the specified ID is not found.</summary>
    [MessageLogging(EventId = 31013, Level = LogLevel.Warning, Message = "DataSet with ID '{id}' not found")]
    public static partial IGenericMessage DataSetByIdNotFound(ILogger logger, Guid id);

    /// <summary>Logs when a Source with the specified ID is not found.</summary>
    [MessageLogging(EventId = 31014, Level = LogLevel.Warning, Message = "Source with ID '{sourceId}' not found")]
    public static partial IGenericMessage SourceByIdNotFound(ILogger logger, Guid sourceId);

    /// <summary>Logs when a container is resolved for a source.</summary>
    [MessageLogging(EventId = 11077, Level = LogLevel.Debug, Message = "Resolved container for source '{sourceId}' using ContainerId '{containerId}'")]
    public static partial IGenericMessage ContainerResolvedForSource(ILogger logger, Guid sourceId, Guid containerId);

    /// <summary>Logs when a source has no ContainerId set.</summary>
    [MessageLogging(EventId = 41006, Level = LogLevel.Warning, Message = "Source '{sourceId}' has no ContainerId set")]
    public static partial IGenericMessage SourceHasNoContainerId(ILogger logger, Guid sourceId);

    /// <summary>Logs when a configuration change is detected.</summary>
    [MessageLogging(EventId = 11078, Level = LogLevel.Debug, Message = "Configuration change detected, rebuilding DataSet indexes")]
    public static partial IGenericMessage ConfigurationChangeDetected(ILogger logger);

    // ============================================================
    // Trace Methods (5170-5179)
    // ============================================================

    /// <summary>Traces entry into DataSetProvider.GetDataSet.</summary>
    [MessageLogging(EventId = 11079, Level = LogLevel.Trace, Message = "Entering DataSetProvider.GetDataSet for '{dataSetName}'")]
    public static partial IGenericMessage TraceGetDataSetEntry(ILogger logger, string dataSetName);

    /// <summary>Traces entry into DataSetProvider.GetAllDataSets.</summary>
    [MessageLogging(EventId = 11080, Level = LogLevel.Trace, Message = "Entering DataSetProvider.GetAllDataSets")]
    public static partial IGenericMessage TraceGetAllDataSetsEntry(ILogger logger);

    /// <summary>Traces entry into DataSetProvider.RegisterDataSet.</summary>
    [MessageLogging(EventId = 11081, Level = LogLevel.Trace, Message = "Entering DataSetProvider.RegisterDataSet for '{dataSetName}'")]
    public static partial IGenericMessage TraceRegisterDataSetEntry(ILogger logger, string dataSetName);

    /// <summary>Traces entry into DataSetProvider.GetDefaultSource.</summary>
    [MessageLogging(EventId = 11082, Level = LogLevel.Trace, Message = "Entering DataSetProvider.GetDefaultSource for DataSet '{dataSetName}'")]
    public static partial IGenericMessage TraceGetDefaultSourceEntry(ILogger logger, string dataSetName);

    /// <summary>Traces entry into DataSetProvider.GetSource.</summary>
    [MessageLogging(EventId = 11083, Level = LogLevel.Trace, Message = "Entering DataSetProvider.GetSource for DataSet '{dataSetName}', Source '{sourceName}'")]
    public static partial IGenericMessage TraceGetSourceEntry(ILogger logger, string dataSetName, string sourceName);

    /// <summary>Traces entry into DataSetProvider.GetFieldMappings.</summary>
    [MessageLogging(EventId = 11084, Level = LogLevel.Trace, Message = "Entering DataSetProvider.GetFieldMappings for DataSet '{dataSetName}', Source '{sourceName}'")]
    public static partial IGenericMessage TraceGetFieldMappingsEntry(ILogger logger, string dataSetName, string sourceName);

    /// <summary>Traces entry into DataSetProvider.GetRecordType.</summary>
    [MessageLogging(EventId = 11085, Level = LogLevel.Trace, Message = "Entering DataSetProvider.GetRecordType for DataSet '{dataSetName}'")]
    public static partial IGenericMessage TraceGetRecordTypeEntry(ILogger logger, string dataSetName);

    /// <summary>Traces entry into DataSetProvider.GetDataSetById.</summary>
    [MessageLogging(EventId = 11086, Level = LogLevel.Trace, Message = "Entering DataSetProvider.GetDataSetById for Id '{id}'")]
    public static partial IGenericMessage TraceGetDataSetByIdEntry(ILogger logger, Guid id);

    /// <summary>Traces entry into DataSetProvider.GetSourceById.</summary>
    [MessageLogging(EventId = 11087, Level = LogLevel.Trace, Message = "Entering DataSetProvider.GetSourceById for SourceId '{sourceId}'")]
    public static partial IGenericMessage TraceGetSourceByIdEntry(ILogger logger, Guid sourceId);

    /// <summary>Traces entry into DataSetProvider.ResolveContainerForSource.</summary>
    [MessageLogging(EventId = 11088, Level = LogLevel.Trace, Message = "Entering DataSetProvider.ResolveContainerForSource for SourceId '{sourceId}'")]
    public static partial IGenericMessage TraceResolveContainerForSourceEntry(ILogger logger, Guid sourceId);

    /// <summary>Logs when cfg DataSet loading fails — database-backed DataSets will be missing.</summary>
    [MessageLogging(EventId = 71019, Level = LogLevel.Warning, Message = "Failed to load cfg DataSets from database: {reason}")]
    public static partial IGenericMessage CfgDataSetLoadFailed(ILogger logger, string reason);

    /// <summary>Logs when a field lookup by name fails on a built dataset.</summary>
    [MessageLogging(EventId = 31015, Level = LogLevel.Warning, Message = "[DataSet] Field '{fieldName}' not found in dataset '{datasetName}'")]
    public static partial IGenericMessage FieldNotFoundInDataSet(ILogger logger, string fieldName, string datasetName);
}
