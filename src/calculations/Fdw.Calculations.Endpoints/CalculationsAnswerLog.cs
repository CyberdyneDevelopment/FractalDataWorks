using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints;

/// <summary>The coded messages the calculations endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("CALCANSWERS")]
public static partial class CalculationsAnswerLog
{

    /// <summary>Logs that The request to list calculation versions was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50001, Level = LogLevel.Warning, Message = "The request to list calculation versions was refused: the caller is not signed in", ClientMessage = "You must be signed in to list calculation versions")]
    public static partial IGenericMessage ListCalculationVersionsNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to delete calculation version was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50002, Level = LogLevel.Warning, Message = "The request to delete calculation version was refused: the caller is not signed in", ClientMessage = "You must be signed in to delete calculation version")]
    public static partial IGenericMessage DeleteCalculationVersionNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to save calculation version was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50003, Level = LogLevel.Warning, Message = "The request to save calculation version was refused: the caller is not signed in", ClientMessage = "You must be signed in to save calculation version")]
    public static partial IGenericMessage SaveCalculationVersionNotSignedIn(ILogger logger);

    /// <summary>Logs that Calculation entity was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Calculation entity was not found, or is not visible to the caller", ClientMessage = "Calculation entity was not found")]
    public static partial IGenericMessage ExecuteCalculationEntityNotFound(ILogger logger);

    /// <summary>Logs that A windowed calculation request left out field.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning, Message = "A windowed calculation request left out {field}", ClientMessage = "{field} is required")]
    public static partial IGenericMessage WindowedCalculationFieldRequired(ILogger logger, string field);

    /// <summary>Logs that A calculation preview named the unknown calculation type 'calculationType'.</summary>
    [MessageLogging(EventId = 20002, Level = LogLevel.Warning, Message = "A calculation preview named the unknown calculation type '{calculationType}'", ClientMessage = "Unknown calculation type: {calculationType}")]
    public static partial IGenericMessage PreviewCalculationUnknownCalculationType(ILogger logger, string calculationType);
}
