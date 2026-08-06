using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging methods for DataSetProvider operations.
/// Provider-specific messages with domain context baked into templates.
/// EventId range: 8920-8939
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class DataSetProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load Data Sets (8920-8921)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the data sets list fails.</summary>
    [MessageLogging(EventId = 91015, Level = LogLevel.Warning,
        Message = "DataSetProvider: Failed to load data sets list")]
    public static partial IGenericMessage LoadDataSetsFailed(
        ILogger logger);

    /// <summary>Logs when loading the data sets list fails with exception.</summary>
    [MessageLogging(EventId = 91016, Level = LogLevel.Warning,
        Message = "DataSetProvider: Failed to load data sets list")]
    public static partial IGenericMessage LoadDataSetsException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Load Data Set Detail (8922-8923)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading data set details fails.</summary>
    [MessageLogging(EventId = 91017, Level = LogLevel.Warning,
        Message = "DataSetProvider: Failed to load data set detail for '{dataSetName}'")]
    public static partial IGenericMessage DataSetDetailLoadFailed(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when loading data set details fails with exception.</summary>
    [MessageLogging(EventId = 91018, Level = LogLevel.Warning,
        Message = "DataSetProvider: Failed to load data set detail")]
    public static partial IGenericMessage DataSetDetailLoadException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Create Data Set (8924-8925)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when creating a data set fails.</summary>
    [MessageLogging(EventId = 91019, Level = LogLevel.Warning,
        Message = "DataSetProvider: Failed to create data set")]
    public static partial IGenericMessage DataSetCreateFailed(
        ILogger logger);

    /// <summary>Logs when creating a data set fails with exception.</summary>
    [MessageLogging(EventId = 91020, Level = LogLevel.Warning,
        Message = "DataSetProvider: Failed to create data set")]
    public static partial IGenericMessage DataSetCreateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Update Data Set (8926-8927)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when updating a data set fails.</summary>
    [MessageLogging(EventId = 91021, Level = LogLevel.Warning,
        Message = "DataSetProvider: Failed to update data set '{dataSetName}'")]
    public static partial IGenericMessage DataSetUpdateFailed(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when updating a data set fails with exception.</summary>
    [MessageLogging(EventId = 91022, Level = LogLevel.Warning,
        Message = "DataSetProvider: Failed to update data set")]
    public static partial IGenericMessage DataSetUpdateException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Delete Data Set (8928-8929)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when deleting a data set fails.</summary>
    [MessageLogging(EventId = 91023, Level = LogLevel.Warning,
        Message = "DataSetProvider: Failed to delete data set '{dataSetName}'")]
    public static partial IGenericMessage DataSetDeleteFailed(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when deleting a data set fails with exception.</summary>
    [MessageLogging(EventId = 91024, Level = LogLevel.Warning,
        Message = "DataSetProvider: Failed to delete data set")]
    public static partial IGenericMessage DataSetDeleteException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Journey Actions (8930-8933)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the Pipeline Builder is started from the DataSet detail with the named DataSet as source.</summary>
    [MessageLogging(EventId = 11030, Level = LogLevel.Information,
        Message = "DataSetProvider: Starting Pipeline Builder with source DataSet '{dataSetName}'")]
    public static partial IGenericMessage StartPipelineFromDataSet(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when the DataSet wizard is opened in derive mode from the named DataSet.</summary>
    [MessageLogging(EventId = 11031, Level = LogLevel.Information,
        Message = "DataSetProvider: Deriving new DataSet from '{dataSetName}'")]
    public static partial IGenericMessage DeriveDataSetFromDetail(
        ILogger logger,
        string dataSetName);
}
