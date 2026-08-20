using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>
/// MessageLogging definitions for Calculation endpoint base classes.
/// EventId range: 1530-1545
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
}
