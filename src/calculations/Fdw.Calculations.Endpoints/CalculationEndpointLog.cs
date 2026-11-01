using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// MessageLogging definitions for Calculation endpoint base classes.
/// EventId range: 1530-1545; 21530-21537, 21539-21549 (Validation category, answered as 400); 61535 (Configuration category); 91002, 91030 (Internal category); 11010 (Debug, server log only).
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS")]
public static partial class CalculationEndpointLog
{
    // List operations (1530-1534)

    /// <summary>Logs that calculation types are being listed.</summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Trace, Message = "Listing available calculation types")]
    public static partial IGenericMessage ListingCalculationTypes(ILogger logger);

    /// <summary>Logs the count of calculation types listed.</summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "Listed {count} calculation types")]
    public static partial IGenericMessage ListedCalculationTypes(ILogger logger, int count);

    /// <summary>Logs that the unified calculation catalog failed to load.</summary>
    [MessageLogging(EventId = 71000, Level = LogLevel.Error, Message = "Failed to load the calculation catalog")]
    public static partial IGenericMessage ListCalculationTypesFailed(ILogger logger);

    /// <summary>Logs that period comparison types are being listed.</summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Trace, Message = "Listing available period comparison types")]
    public static partial IGenericMessage ListingPeriodComparisonTypes(ILogger logger);

    /// <summary>Logs the count of period comparison types listed.</summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "Listed {count} period comparison types")]
    public static partial IGenericMessage ListedPeriodComparisonTypes(ILogger logger, int count);

    // Execute operations (1535-1539)

    /// <summary>Logs that a calculation is being executed.</summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace, Message = "Executing calculation '{calculationType}' on data set '{dataSetName}'")]
    public static partial IGenericMessage ExecutingCalculation(ILogger logger, string dataSetName, string calculationType);

    /// <summary>Logs that a calculation was executed successfully with duration.</summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "Calculation '{calculationType}' executed on data set '{dataSetName}' in {durationMs}ms")]
    public static partial IGenericMessage CalculationExecuted(ILogger logger, string dataSetName, string calculationType, long durationMs);

    /// <summary>Logs that a calculation failed with an exception.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Calculation '{calculationType}' failed on data set '{dataSetName}'")]
    public static partial IGenericMessage CalculationFailed(ILogger logger, Exception ex, string dataSetName, string calculationType);

    /// <summary>Logs a validation failure with reason.</summary>
    [MessageLogging(EventId = 21000, Level = LogLevel.Warning, Message = "Validation failed: {reason}")]
    public static partial IGenericMessage ValidationFailed(ILogger logger, string reason);

    // Preview operations (1540-1544)

    /// <summary>Logs that a calculation preview is starting.</summary>
    [MessageLogging(EventId = 11006, Level = LogLevel.Debug, Message = "Previewing calculation '{calculationType}'")]
    public static partial IGenericMessage PreviewingCalculation(ILogger logger, string calculationType);

    /// <summary>Logs that a calculation preview completed successfully.</summary>
    [MessageLogging(EventId = 11007, Level = LogLevel.Information, Message = "Calculation preview completed for '{calculationType}'")]
    public static partial IGenericMessage CalculationPreviewCompleted(ILogger logger, string calculationType);

    /// <summary>Logs that a calculation preview failed with an exception.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Error, Message = "Calculation preview failed for '{calculationType}'")]
    public static partial IGenericMessage CalculationPreviewFailed(ILogger logger, Exception ex, string calculationType);

    /// <summary>Logs that window functions are being listed.</summary>
    [MessageLogging(EventId = 11008, Level = LogLevel.Trace, Message = "Listing available window functions")]
    public static partial IGenericMessage ListingWindowFunctions(ILogger logger);

    /// <summary>Logs the count of window functions listed.</summary>
    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "Listed {count} window functions")]
    public static partial IGenericMessage ListedWindowFunctions(ILogger logger, int count);

    /// <summary>A windowed calculation request named a window function that is not offered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="windowFunction">The function name that was sent.</param>
    /// <param name="offered">The offered function names.</param>
    [MessageLogging(EventId = 21533, Level = LogLevel.Warning,
        Message = "Window function '{windowFunction}' is not offered; the offered functions are {offered}",
        ClientMessage = "The window function '{windowFunction}' is not offered; the offered functions are {offered}")]
    public static partial IGenericMessage WindowFunctionNotOffered(ILogger logger, string windowFunction, string offered);

    /// <summary>A windowed calculation request named a calculation that is not a windowed calculation.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="calculationName">The calculation's name.</param>
    /// <param name="calculationEntityType">The calculation's actual type.</param>
    [MessageLogging(EventId = 21534, Level = LogLevel.Warning,
        Message = "Calculation '{calculationName}' is a '{calculationEntityType}' calculation, not a windowed one",
        ClientMessage = "Calculation '{calculationName}' is a '{calculationEntityType}' calculation, not a windowed one")]
    public static partial IGenericMessage CalculationNotWindowed(ILogger logger, string calculationName, string calculationEntityType);

    /// <summary>A calculation request's configuration carried a member sent as null.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="calculationEntityType">The calculation entity type.</param>
    /// <param name="member">The configuration member that was sent as null.</param>
    [MessageLogging(EventId = 21546, Level = LogLevel.Warning,
        Message = "The configuration for calculation type '{calculationEntityType}' has member '{member}' sent as null; a member is sent with a value or left out",
        ClientMessage = "Configuration member '{member}' of calculation type '{calculationEntityType}' cannot be null: send a value, or leave it out to keep the stored one")]
    public static partial IGenericMessage ConfigurationMemberNull(ILogger logger, string calculationEntityType, string member);

    /// <summary>A calculation execute request named a field the data set's rows do not carry.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="fieldName">The field the request named.</param>
    /// <param name="dataSetName">The data set the request named.</param>
    [MessageLogging(EventId = 21540, Level = LogLevel.Warning,
        Message = "Calculation execute was asked for field '{fieldName}', which the rows of data set '{dataSetName}' do not carry",
        ClientMessage = "Data set '{dataSetName}' has no field '{fieldName}'")]
    public static partial IGenericMessage FieldNotInDataSet(ILogger logger, string fieldName, string dataSetName);

    /// <summary>A calculation execute request named a field of a data set that holds a value that is not a number.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="fieldName">The field the request named.</param>
    /// <param name="dataSetName">The data set the request named.</param>
    [MessageLogging(EventId = 21541, Level = LogLevel.Warning,
        Message = "Field '{fieldName}' of data set '{dataSetName}' holds a value that is not a number",
        ClientMessage = "Field '{fieldName}' of data set '{dataSetName}' holds a value that is not a number")]
    public static partial IGenericMessage FieldValueNotNumber(ILogger logger, string fieldName, string dataSetName);

    /// <summary>A calculation execute request named a data set, and the host has no data set row reader or data gateway registered.</summary>
    /// <param name="logger">The logger.</param>
    [MessageLogging(EventId = 61535, Level = LogLevel.Error,
        Message = "Calculation execute was asked to read a data set, and this host has no IDataSetRowReader or IDataGatewayProvider registered")]
    public static partial IGenericMessage DataSetReaderUnavailable(ILogger logger);

    /// <summary>A calculation request declared an input of a kind the calculation service does not have.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="inputAlias">The alias of the input.</param>
    /// <param name="inputKind">The kind that was sent.</param>
    /// <param name="offered">The offered kind names.</param>
    [MessageLogging(EventId = 21536, Level = LogLevel.Warning,
        Message = "Calculation input '{inputAlias}' has input kind '{inputKind}', which is not offered; the offered kinds are {offered}",
        ClientMessage = "Calculation input '{inputAlias}' has input kind '{inputKind}', which is not offered; the offered kinds are {offered}")]
    public static partial IGenericMessage InputKindUnknown(ILogger logger, string inputAlias, string inputKind, string offered);

    /// <summary>A calculation request declared an input that sends a member its kind does not take.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="inputAlias">The alias of the input.</param>
    /// <param name="inputKind">The kind of the input.</param>
    /// <param name="member">The member that was sent.</param>
    /// <param name="declared">The members the kind takes.</param>
    [MessageLogging(EventId = 21547, Level = LogLevel.Warning,
        Message = "Calculation input '{inputAlias}' of kind '{inputKind}' sends member '{member}', which the kind does not take; it takes {declared}",
        ClientMessage = "Calculation input '{inputAlias}' of kind '{inputKind}' sends member '{member}', which the kind does not take; it takes {declared}")]
    public static partial IGenericMessage InputMemberNotDeclared(ILogger logger, string inputAlias, string inputKind, string member, string declared);

    /// <summary>A calculation request declared a Scalar input with a value type the calculation service does not have.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="inputAlias">The alias of the input.</param>
    /// <param name="scalarValueType">The value type that was sent.</param>
    /// <param name="offered">The offered value type names.</param>
    [MessageLogging(EventId = 21537, Level = LogLevel.Warning,
        Message = "Calculation input '{inputAlias}' has scalar value type '{scalarValueType}', which is not offered; the offered types are {offered}",
        ClientMessage = "Calculation input '{inputAlias}' has scalar value type '{scalarValueType}', which is not offered; the offered types are {offered}")]
    public static partial IGenericMessage ScalarValueTypeUnknown(ILogger logger, string inputAlias, string scalarValueType, string offered);

    /// <summary>A calculation request declared an input that leaves out a member its kind requires.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="inputAlias">The alias of the input.</param>
    /// <param name="inputKind">The kind of the input.</param>
    /// <param name="member">The required member that is null or empty.</param>
    [MessageLogging(EventId = 21548, Level = LogLevel.Warning,
        Message = "Calculation input '{inputAlias}' of kind '{inputKind}' needs member '{member}', which is missing or empty",
        ClientMessage = "Calculation input '{inputAlias}' of kind '{inputKind}' needs member '{member}', which is missing or empty")]
    public static partial IGenericMessage InputMemberRequired(ILogger logger, string inputAlias, string inputKind, string member);

    /// <summary>A calculation request declared a Scalar input whose value is not a value of its value type.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="inputAlias">The alias of the input.</param>
    /// <param name="scalarValueType">The value type of the input.</param>
    /// <param name="scalarSerializedValue">The value that was sent.</param>
    [MessageLogging(EventId = 21549, Level = LogLevel.Warning,
        Message = "Calculation input '{inputAlias}' has scalar value '{scalarSerializedValue}', which is not a value of type '{scalarValueType}'",
        ClientMessage = "Calculation input '{inputAlias}' has scalar value '{scalarSerializedValue}', which is not a value of type '{scalarValueType}'")]
    public static partial IGenericMessage ScalarValueInvalid(ILogger logger, string inputAlias, string scalarValueType, string scalarSerializedValue);

    /// <summary>A calculation request's configuration carried a member the calculation entity type does not declare.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="calculationEntityType">The calculation entity type.</param>
    /// <param name="member">The configuration member that was sent.</param>
    /// <param name="declared">The members the type declares.</param>
    [MessageLogging(EventId = 21539, Level = LogLevel.Warning,
        Message = "The configuration for calculation type '{calculationEntityType}' has member '{member}', which the type does not declare; the declared members are {declared}",
        ClientMessage = "The configuration for calculation type '{calculationEntityType}' has member '{member}', which the type does not declare; the declared members are {declared}")]
    public static partial IGenericMessage ConfigurationMemberNotDeclared(
        ILogger logger, string calculationEntityType, string member, string declared);

    /// <summary>Logs that an unknown calculation type was requested.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning, Message = "Unknown calculation type '{calculationType}'")]
    public static partial IGenericMessage UnknownCalculationType(ILogger logger, string calculationType);

    /// <summary>A calculation request named a calculation entity type that is not registered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="calculationEntityType">The type name that was sent.</param>
    [MessageLogging(EventId = 21530, Level = LogLevel.Warning,
        Message = "Unknown calculation entity type '{calculationEntityType}'",
        ClientMessage = "Calculation entity type '{calculationEntityType}' is not a registered type")]
    public static partial IGenericMessage TypedConfigurationUnknownType(ILogger logger, string calculationEntityType);

    /// <summary>A calculation request's configuration could not be read as its type's configuration class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="calculationEntityType">The calculation entity type.</param>
    /// <param name="configurationType">The configuration class the body was read as.</param>
    /// <param name="reason">Why it could not be read.</param>
    [MessageLogging(EventId = 21531, Level = LogLevel.Warning,
        Message = "The configuration for calculation type '{calculationEntityType}' could not be read as {configurationType}: {reason}",
        ClientMessage = "The configuration for calculation type '{calculationEntityType}' could not be read: {reason}")]
    public static partial IGenericMessage TypedConfigurationUnreadable(
        ILogger logger, string calculationEntityType, string configurationType, string reason);

    /// <summary>An update changed a calculation's type without sending the new type's configuration.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="fromType">The calculation's current type.</param>
    /// <param name="toType">The type the update asked for.</param>
    [MessageLogging(EventId = 21532, Level = LogLevel.Warning,
        Message = "Changing a calculation from '{fromType}' to '{toType}' needs the new type's configuration; none was sent",
        ClientMessage = "Changing a calculation from '{fromType}' to '{toType}' needs the new type's configuration; send it in 'configuration'")]
    public static partial IGenericMessage TypedConfigurationRequiredForTypeChange(ILogger logger, string fromType, string toType);

    /// <summary>The framework's own text for a configuration that could not be bound; written to the server log and never returned to a caller.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="calculationEntityType">The calculation entity type.</param>
    /// <param name="detail">The framework's description of what failed.</param>
    [MessageLogging(EventId = 11010, Level = LogLevel.Debug,
        Message = "The configuration for calculation type '{calculationEntityType}' could not be bound: {detail}")]
    public static partial IGenericMessage TypedConfigurationBindFailed(ILogger logger, string calculationEntityType, string detail);

    /// <summary>A persisted calculation entity has no type, which a calculation cannot be without.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="entityName">The calculation entity's name.</param>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "Calculation entity '{entityName}' has no type (Implementation)")]
    public static partial IGenericMessage CalculationEntityMissingType(ILogger logger, string entityName);

    /// <summary>A windowed calculation's result did not carry the partition and row counts the response states.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="calculationName">The calculation that was executed.</param>
    [MessageLogging(EventId = 91030, Level = LogLevel.Error,
        Message = "The result of windowed calculation '{calculationName}' does not state its PartitionCount and RowCount")]
    public static partial IGenericMessage WindowedResultCountsMissing(ILogger logger, string calculationName);
}
