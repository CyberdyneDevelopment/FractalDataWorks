using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Calculations.Aggregations;

/// <summary>
/// Static logger class for Aggregation calculation operations.
/// Uses MessageLogging infrastructure for high-performance logging that returns IGenericMessage.
/// </summary>
[MessageLoggingTypeCode("AGGREGATIONS")]
public static partial class AggregationCalculationLogger
{
    /// <summary>Logs that an aggregation calculation is being executed.</summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Executing aggregation calculation: {calculationName}")]
    public static partial IGenericMessage ExecutingAggregation(ILogger logger, string calculationName);

    /// <summary>Logs that data is being retrieved from a connection and container.</summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Retrieving data from connection '{connectionName}', container '{containerName}'")]
    public static partial IGenericMessage RetrievingData(ILogger logger, string? connectionName, string? containerName);

    /// <summary>Logs the number of records retrieved for aggregation.</summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "Retrieved {recordCount} records for aggregation")]
    public static partial IGenericMessage DataRetrieved(ILogger logger, long recordCount);

    /// <summary>Logs that an aggregation is being calculated on a field.</summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "Calculating {aggregationType} on field '{fieldName}'")]
    public static partial IGenericMessage CalculatingAggregation(ILogger logger, string aggregationType, string? fieldName);

    /// <summary>Logs that an aggregation calculation has completed successfully.</summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Aggregation completed: {aggregationType} = {result}")]
    public static partial IGenericMessage AggregationCompleted(ILogger logger, string aggregationType, decimal result);

    /// <summary>Logs that no records were found for aggregation (call site returns Success(default)).</summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Debug,
        Message = "No records found for aggregation")]
    public static partial IGenericMessage NoRecordsFound(ILogger logger);

    /// <summary>Logs an error that data retrieval failed.</summary>
    [MessageLogging(
        EventId = 71000,
        Level = LogLevel.Error,
        Message = "Data retrieval failed: {errorMessage}")]
    public static partial IGenericMessage DataRetrievalFailed(ILogger logger, string errorMessage);

    /// <summary>Logs an error that a field was not found on the record type.</summary>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Error,
        Message = "Field '{fieldName}' not found on record type")]
    public static partial IGenericMessage FieldNotFound(ILogger logger, string? fieldName);

    /// <summary>Logs an error that a field value could not be converted to decimal.</summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Field '{fieldName}' value could not be converted to decimal")]
    public static partial IGenericMessage FieldConversionFailed(ILogger logger, string? fieldName);

    /// <summary>Logs a warning that configuration validation failed.</summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Configuration validation failed: {errorMessage}")]
    public static partial IGenericMessage ValidationFailed(ILogger logger, string errorMessage);

    /// <summary>Logs an error that no field accessor was found for a type.</summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Error,
        Message = "No field accessor found for type '{typeName}'. Add [GenerateMapper] attribute.")]
    public static partial IGenericMessage FieldAccessorNotFound(ILogger logger, string typeName);
}
