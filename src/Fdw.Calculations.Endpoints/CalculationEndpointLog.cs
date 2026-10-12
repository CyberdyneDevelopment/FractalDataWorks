using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// MessageLogging definitions for Calculation endpoint base classes.
/// EventId range: 1530-1545; 21530-21532 (Validation category, answered as 400); 91002 (Internal category).
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

    /// <summary>Logs that an unknown calculation type was requested.</summary>
    [MessageLogging(EventId = 21001, Level = LogLevel.Warning, Message = "Unknown calculation type '{calculationType}'")]
    public static partial IGenericMessage UnknownCalculationType(ILogger logger, string calculationType);

    /// <summary>A calculation request named a calculation entity type that is not registered.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="calculationEntityType">The type name that was sent.</param>
    [MessageLogging(EventId = 21530, Level = LogLevel.Warning,
        Message = "Unknown calculation entity type '{calculationEntityType}'")]
    public static partial IGenericMessage TypedConfigurationUnknownType(ILogger logger, string calculationEntityType);

    /// <summary>A calculation request's configuration could not be read as its type's configuration class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="calculationEntityType">The calculation entity type.</param>
    /// <param name="configurationType">The configuration class the body was read as.</param>
    /// <param name="reason">Why it could not be read.</param>
    [MessageLogging(EventId = 21531, Level = LogLevel.Warning,
        Message = "The configuration for calculation type '{calculationEntityType}' could not be read as {configurationType}: {reason}")]
    public static partial IGenericMessage TypedConfigurationUnreadable(
        ILogger logger, string calculationEntityType, string configurationType, string reason);

    /// <summary>An update changed a calculation's type without sending the new type's configuration.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="fromType">The calculation's current type.</param>
    /// <param name="toType">The type the update asked for.</param>
    [MessageLogging(EventId = 21532, Level = LogLevel.Warning,
        Message = "Changing a calculation from '{fromType}' to '{toType}' needs the new type's configuration; none was sent")]
    public static partial IGenericMessage TypedConfigurationRequiredForTypeChange(ILogger logger, string fromType, string toType);

    /// <summary>A persisted calculation entity has no type, which a calculation cannot be without.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="entityName">The calculation entity's name.</param>
    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "Calculation entity '{entityName}' has no type (Implementation)")]
    public static partial IGenericMessage CalculationEntityMissingType(ILogger logger, string entityName);
}
