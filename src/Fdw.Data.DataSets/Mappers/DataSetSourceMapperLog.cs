using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataSets;

/// <summary>
/// Static logger class for data set source mapper operations.
/// EventId range: 9380-9389.
/// </summary>
[MessageLoggingTypeCode("DATASETS")]
public static partial class DataSetSourceMapperLog
{
    // ========================================================================
    // Mapper Lifecycle (9380-9384)
    // ========================================================================

    /// <summary>
    /// Logs that mapper record extraction is starting.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Debug,
        Message = "Starting record extraction with mapper '{mapperName}' using selector '{recordSelector}'")]
    public static partial IGenericMessage ExtractingRecords(ILogger logger, string mapperName, string recordSelector);

    /// <summary>
    /// Logs that mapper record extraction completed successfully.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Mapper '{mapperName}' extracted {recordCount} records")]
    public static partial IGenericMessage RecordsExtracted(ILogger logger, string mapperName, int recordCount);

    /// <summary>
    /// Logs that the payload could not be parsed.
    /// </summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Mapper '{mapperName}' failed to parse payload: {errorMessage}")]
    public static partial IGenericMessage PayloadParseFailed(ILogger logger, string mapperName, string errorMessage);

    /// <summary>
    /// Logs that the record selector expression could not be evaluated.
    /// </summary>
    [MessageLogging(
        EventId = 91001,
        Level = LogLevel.Error,
        Message = "Record selector '{recordSelector}' evaluation failed for mapper '{mapperName}': {errorMessage}")]
    public static partial IGenericMessage RecordSelectorFailed(ILogger logger, string recordSelector, string mapperName, string errorMessage);

    /// <summary>
    /// Logs that a field XPath expression could not be evaluated.
    /// </summary>
    [MessageLogging(
        EventId = 91002,
        Level = LogLevel.Error,
        Message = "Field XPath '{fieldXPath}' for logical field '{logicalFieldName}' failed in mapper '{mapperName}': {errorMessage}")]
    public static partial IGenericMessage FieldExtractionFailed(ILogger logger, string fieldXPath, string logicalFieldName, string mapperName, string errorMessage);

    // ========================================================================
    // Mapper Trace (9385-9389)
    // ========================================================================

    /// <summary>
    /// Logs that namespace stripping is being applied to the XML payload.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Stripping XML namespaces from payload for mapper '{mapperName}'")]
    public static partial IGenericMessage StrippingNamespaces(ILogger logger, string mapperName);

    /// <summary>
    /// Logs the record selector XPath evaluation result count.
    /// </summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "Record selector '{recordSelector}' matched {elementCount} elements")]
    public static partial IGenericMessage RecordSelectorMatched(ILogger logger, string recordSelector, int elementCount);

    /// <summary>
    /// Logs that mapper type was not found by name.
    /// </summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Warning,
        Message = "Mapper type '{mapperTypeName}' not found in DataSetSourceMapperTypes")]
    public static partial IGenericMessage MapperTypeNotFound(ILogger logger, string mapperTypeName);

    // ========================================================================
    // Transform Chain (9388-9390)
    // ========================================================================

    /// <summary>
    /// Logs that a transform type was not found in DataTransformerTypes.
    /// </summary>
    [MessageLogging(
        EventId = 31001,
        Level = LogLevel.Error,
        Message = "Transform type '{transformType}' not found in DataTransformerTypes for field '{logicalFieldName}'")]
    public static partial IGenericMessage TransformTypeNotFound(ILogger logger, string transformType, string logicalFieldName);

    /// <summary>
    /// Logs that a transform type is not a FieldTransformerTypeBase.
    /// </summary>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Error,
        Message = "Transform type '{transformType}' for field '{logicalFieldName}' is not a FieldTransformerTypeBase")]
    public static partial IGenericMessage TransformTypeNotFieldTransformer(ILogger logger, string transformType, string logicalFieldName);

    /// <summary>
    /// Logs that a transform step failed during field value processing.
    /// </summary>
    [MessageLogging(
        EventId = 91003,
        Level = LogLevel.Error,
        Message = "Transform '{transformType}' at ordinal {ordinal} failed for field '{logicalFieldName}': {errorMessage}")]
    public static partial IGenericMessage TransformStepFailed(ILogger logger, string transformType, int ordinal, string logicalFieldName, string errorMessage);

    /// <summary>
    /// Logs that the DataSet mapper type is a stub pending full compound/federated execution engine integration.
    /// </summary>
    [MessageLogging(
        EventId = 91004,
        Level = LogLevel.Error,
        Message = "DataSet mapper '{mapper}' is not yet fully implemented; compound/federated execution requires the upstream DataSet execution engine")]
    public static partial IGenericMessage DataSetMapperNotYetImplemented(ILogger logger, string mapper);
}
