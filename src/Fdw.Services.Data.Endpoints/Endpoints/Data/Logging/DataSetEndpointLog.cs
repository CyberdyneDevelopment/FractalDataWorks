using System;
using Fdw.Messages;
using Fdw.MessageLogging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints.Logging;

/// <summary>
/// MessageLogging for DataSet endpoint base operations.
/// EventId range: 4400-4430
/// </summary>
[MessageLoggingTypeCode("DATAENDPOINTS")]
public static partial class DataSetEndpointLog
{
    // List (4400-4401)

    /// <summary>Logs querying data sets from a connection.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug, Message = "Querying data sets from '{connectionName}'")]
    public static partial IGenericMessage QueryingDataSets(ILogger logger, string connectionName);

    /// <summary>Logs the count of data sets found.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Found {count} data sets")]
    public static partial IGenericMessage DataSetsLoaded(ILogger logger, int count);

    // Get (4405-4406)

    /// <summary>Logs loading a specific data set by name.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug, Message = "Loading data set '{name}' from '{connectionName}'")]
    public static partial IGenericMessage LoadingDataSet(ILogger logger, string name, string connectionName);

    /// <summary>Logs that a data set was not found.</summary>
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning, Message = "Data set '{name}' not found")]
    public static partial IGenericMessage DataSetNotFound(ILogger logger, string name);

    // Create (4410-4413)

    /// <summary>Logs the start of data set creation.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Trace, Message = "Creating data set '{name}'")]
    public static partial IGenericMessage CreatingDataSet(ILogger logger, string name);

    /// <summary>Logs successful data set creation.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Information, Message = "Data set '{name}' created successfully")]
    public static partial IGenericMessage DataSetCreated(ILogger logger, string name);

    /// <summary>Logs a data set creation failure.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Failed to create data set '{name}': {reason}")]
    public static partial IGenericMessage DataSetCreateFailed(ILogger logger, string name, string reason);

    /// <summary>Logs that a data set already exists.</summary>
    [MessageLogging(EventId = 41000, Level = LogLevel.Warning, Message = "Data set '{name}' already exists")]
    public static partial IGenericMessage DataSetAlreadyExists(ILogger logger, string name);

    // Update (4415-4417)

    /// <summary>Logs the start of data set update.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace, Message = "Updating data set '{name}'")]
    public static partial IGenericMessage UpdatingDataSet(ILogger logger, string name);

    /// <summary>Logs successful data set update.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Information, Message = "Data set '{name}' updated successfully")]
    public static partial IGenericMessage DataSetUpdated(ILogger logger, string name);

    /// <summary>Logs a data set update failure.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Failed to update data set '{name}': {reason}")]
    public static partial IGenericMessage DataSetUpdateFailed(ILogger logger, string name, string reason);

    // Delete (4420-4422)

    /// <summary>Logs the start of data set deletion.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Trace, Message = "Deleting data set '{name}'")]
    public static partial IGenericMessage DeletingDataSet(ILogger logger, string name);

    /// <summary>Logs successful data set deletion.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Information, Message = "Data set '{name}' deleted successfully")]
    public static partial IGenericMessage DataSetDeleted(ILogger logger, string name);

    /// <summary>Logs a data set deletion failure.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "Failed to delete data set '{name}': {reason}")]
    public static partial IGenericMessage DataSetDeleteFailed(ILogger logger, string name, string reason);

    // Fields & Sources (4425-4426)

    /// <summary>Logs loading fields for a data set.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Debug, Message = "Loading fields for data set '{name}'")]
    public static partial IGenericMessage LoadingFields(ILogger logger, string name);

    /// <summary>Logs loading sources for a data set.</summary>
    [MessageLogging(EventId = 11010, Level = LogLevel.Debug, Message = "Loading sources for data set '{name}'")]
    public static partial IGenericMessage LoadingSources(ILogger logger, string name);

    // Preview (4427-4429)

    /// <summary>Logs a failure loading sources for preview.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Warning, Message = "Failed to load sources for data set '{name}' preview")]
    public static partial IGenericMessage PreviewSourceLoadFailed(ILogger logger, string name);

    /// <summary>Logs a failure fetching preview rows.</summary>
    [MessageLogging(EventId = 71001, Level = LogLevel.Warning, Message = "Failed to fetch preview rows for data set '{name}'")]
    public static partial IGenericMessage PreviewRowsFetchFailed(ILogger logger, string name);

    // Writer (4430)

    /// <summary>Logs that the configuration writer is unavailable.</summary>
    [MessageLogging(EventId = 61000, Level = LogLevel.Error, Message = "Configuration writer unavailable for data sets")]
    public static partial IGenericMessage WriterUnavailable(ILogger logger);

    // Preview (4431-4434)

    /// <summary>Logs that the DataSet name is required for a preview request.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning, Message = "DataSet preview request rejected: name is required")]
    public static partial IGenericMessage DataSetNameRequired(ILogger logger);

    /// <summary>Logs the start of a DataSet data preview.</summary>
    [MessageLogging(EventId = 11011, Level = LogLevel.Debug, Message = "Previewing data set '{name}' page {page} (pageSize={pageSize})")]
    public static partial IGenericMessage PreviewingDataSet(ILogger logger, string name, int page, int pageSize);

    /// <summary>Logs a DataSet preview failure from the data gateway.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error, Message = "DataSet preview failed for '{name}'")]
    public static partial IGenericMessage DataSetPreviewFailed(ILogger logger, string name);

    /// <summary>Logs an unexpected exception during DataSet preview.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error, Message = "Unexpected error previewing DataSet '{name}'")]
    public static partial IGenericMessage DataSetPreviewException(ILogger logger, Exception ex, string name);

    // System protection (4429)

    /// <summary>Logs when a modification is rejected because the data set is a system configuration.</summary>
    [MessageLogging(EventId = 41001, Level = LogLevel.Warning, Message = "Rejected modification of system data set '{dataSetName}' — system configurations are read-only")]
    public static partial IGenericMessage SystemDataSetReadOnly(ILogger logger, string dataSetName);

    // POST query (4435-4439)

    /// <summary>Logs the start of a POST-body DataSet query.</summary>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace, Message = "POST querying DataSet '{dataSetName}' with {filterCount} filter(s), skip={skip}, take={take}")]
    public static partial IGenericMessage PostQueryingDataSet(ILogger logger, string dataSetName, int filterCount, int skip, int take);

    /// <summary>Logs that a filter field in the POST body is not a known DataSet field.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning, Message = "POST query filter field '{fieldName}' not found in DataSet '{dataSetName}', ignoring")]
    public static partial IGenericMessage PostQueryUnknownFilterField(ILogger logger, string dataSetName, string fieldName);

    /// <summary>Logs completion of a POST-body DataSet query.</summary>
    [MessageLogging(EventId = 11013, Level = LogLevel.Information, Message = "POST DataSet query '{dataSetName}' returned {rowCount} rows (hasMore={hasMore})")]
    public static partial IGenericMessage PostQueryCompleted(ILogger logger, string dataSetName, int rowCount, bool hasMore);

    /// <summary>Logs a DataGateway failure during a POST-body DataSet query.</summary>
    [MessageLogging(EventId = 71002, Level = LogLevel.Error, Message = "POST DataSet query failed for '{dataSetName}': {message}")]
    public static partial IGenericMessage PostQueryFailed(ILogger logger, string dataSetName, string message);

}
