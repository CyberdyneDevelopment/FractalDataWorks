using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Data.Components.Logging;

/// <summary>
/// MessageLogging methods for DataSetDetailProvider operations.
/// EventId range: 9700-9729
/// </summary>
[MessageLoggingTypeCode("DATACOMPONENTS")]
public static partial class DataSetDetailProviderLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Load DataSet Detail (9700-9701)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when loading the DataSet detail fails.</summary>
    [MessageLogging(EventId = 71018, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to load DataSet detail for '{dataSetName}'")]
    public static partial IGenericMessage LoadDataSetDetailFailed(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when loading the DataSet detail fails with exception.</summary>
    [MessageLogging(EventId = 71019, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception loading DataSet detail")]
    public static partial IGenericMessage LoadDataSetDetailException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Add Source (9702-9703)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when adding a source to the working set fails.</summary>
    [MessageLogging(EventId = 71020, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to add source '{sourceName}'")]
    public static partial IGenericMessage AddSourceFailed(
        ILogger logger,
        string sourceName);

    /// <summary>Logs when adding a source to the working set fails with exception.</summary>
    [MessageLogging(EventId = 71021, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception adding source")]
    public static partial IGenericMessage AddSourceException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Remove Source (9704-9705)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when removing a source from the working set fails.</summary>
    [MessageLogging(EventId = 71022, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to remove source '{sourceName}'")]
    public static partial IGenericMessage RemoveSourceFailed(
        ILogger logger,
        string sourceName);

    /// <summary>Logs when removing a source from the working set fails with exception.</summary>
    [MessageLogging(EventId = 71023, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception removing source")]
    public static partial IGenericMessage RemoveSourceException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Add Join (9706-9707)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when adding a join definition to the working set fails.</summary>
    [MessageLogging(EventId = 71024, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to add join between '{leftSource}' and '{rightSource}'")]
    public static partial IGenericMessage AddJoinFailed(
        ILogger logger,
        string leftSource,
        string rightSource);

    /// <summary>Logs when adding a join definition fails with exception.</summary>
    [MessageLogging(EventId = 71025, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception adding join")]
    public static partial IGenericMessage AddJoinException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Remove Join (9708-9709)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when removing a join definition from the working set fails.</summary>
    [MessageLogging(EventId = 71026, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to remove join '{joinId}'")]
    public static partial IGenericMessage RemoveJoinFailed(
        ILogger logger,
        string joinId);

    /// <summary>Logs when removing a join definition fails with exception.</summary>
    [MessageLogging(EventId = 71027, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception removing join")]
    public static partial IGenericMessage RemoveJoinException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Add Calculation (9710-9711)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when adding a calculated field to the working set fails.</summary>
    [MessageLogging(EventId = 71028, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to add calculation '{calculationName}'")]
    public static partial IGenericMessage AddCalculationFailed(
        ILogger logger,
        string calculationName);

    /// <summary>Logs when adding a calculated field fails with exception.</summary>
    [MessageLogging(EventId = 71029, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception adding calculation")]
    public static partial IGenericMessage AddCalculationException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Remove Calculation (9712-9713)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when removing a calculated field from the working set fails.</summary>
    [MessageLogging(EventId = 71030, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to remove calculation '{calculationName}'")]
    public static partial IGenericMessage RemoveCalculationFailed(
        ILogger logger,
        string calculationName);

    /// <summary>Logs when removing a calculated field fails with exception.</summary>
    [MessageLogging(EventId = 71031, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception removing calculation")]
    public static partial IGenericMessage RemoveCalculationException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Add Aggregation (9714-9715)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when adding an aggregation definition to the working set fails.</summary>
    [MessageLogging(EventId = 71032, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to add aggregation '{aggregationName}'")]
    public static partial IGenericMessage AddAggregationFailed(
        ILogger logger,
        string aggregationName);

    /// <summary>Logs when adding an aggregation definition fails with exception.</summary>
    [MessageLogging(EventId = 91007, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception adding aggregation")]
    public static partial IGenericMessage AddAggregationException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Remove Aggregation (9716-9717)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when removing an aggregation definition from the working set fails.</summary>
    [MessageLogging(EventId = 91008, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to remove aggregation '{aggregationName}'")]
    public static partial IGenericMessage RemoveAggregationFailed(
        ILogger logger,
        string aggregationName);

    /// <summary>Logs when removing an aggregation definition fails with exception.</summary>
    [MessageLogging(EventId = 91009, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception removing aggregation")]
    public static partial IGenericMessage RemoveAggregationException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Preview (9718-9719)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when executing a workbench preview fails.</summary>
    [MessageLogging(EventId = 91010, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Preview failed for DataSet '{dataSetName}'")]
    public static partial IGenericMessage PreviewFailed(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when executing a workbench preview fails with exception.</summary>
    [MessageLogging(EventId = 91011, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception executing preview")]
    public static partial IGenericMessage PreviewException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Save DataSet (9720-9721)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when saving the composed DataSet back to ConfigurationDb fails.</summary>
    [MessageLogging(EventId = 91012, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to save DataSet '{dataSetName}'")]
    public static partial IGenericMessage SaveDataSetFailed(
        ILogger logger,
        string dataSetName);

    /// <summary>Logs when saving the composed DataSet fails with exception.</summary>
    [MessageLogging(EventId = 91013, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Exception saving DataSet")]
    public static partial IGenericMessage SaveDataSetException(
        ILogger logger,
        Exception exception);

    // ═══════════════════════════════════════════════════════════════════════════
    // Missing Parameter Guard (9722)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs when the DataSetName parameter is null or empty — provider cannot initialize.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Error,
        Message = "DataSetDetailProvider: DataSetName parameter is required but was not provided")]
    public static partial IGenericMessage DataSetNameRequired(
        ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Aggregation Function Names (9723-9725)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Logs the start of loading AggregationFunctions TypeCollection values.</summary>
    [MessageLogging(EventId = 11028, Level = LogLevel.Trace,
        Message = "DataSetDetailProvider: Loading AggregationFunctions TypeCollection values")]
    public static partial IGenericMessage LoadingAggregationFunctions(
        ILogger logger);

    /// <summary>Logs when AggregationFunctions values have been loaded.</summary>
    [MessageLogging(EventId = 11029, Level = LogLevel.Information,
        Message = "DataSetDetailProvider: Loaded {count} AggregationFunctions values")]
    public static partial IGenericMessage LoadedAggregationFunctions(
        ILogger logger,
        int count);

    /// <summary>Logs when loading AggregationFunctions TypeCollection values fails.</summary>
    [MessageLogging(EventId = 91014, Level = LogLevel.Warning,
        Message = "DataSetDetailProvider: Failed to load AggregationFunctions TypeCollection values")]
    public static partial IGenericMessage LoadAggregationFunctionsFailed(
        ILogger logger,
        Exception exception);
}
