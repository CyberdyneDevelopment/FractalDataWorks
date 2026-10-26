using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// MessageLogging for the versions of a calculation held at places (FDW-2560).
/// EventId ranges: 12790-12792 (done), 22790 (a version whose text is not acceptable, 400), 32790-32791 (not found, 404).
/// </summary>
[MessageLoggingTypeCode("ENDPOINTS")]
public static partial class CalculationVersionLog
{
    /// <summary>Logs that the versions of a calculation were listed.</summary>
    [MessageLogging(EventId = 12790, Level = LogLevel.Debug, Message = "Calculation '{calculation}' has {count} versions at or above {place}")]
    public static partial IGenericMessage Listed(ILogger logger, string calculation, int count, string place);

    /// <summary>Logs that a version of a calculation was saved at a place.</summary>
    [MessageLogging(EventId = 12791, Level = LogLevel.Information, Message = "A version of calculation '{calculation}' was saved at {place}")]
    public static partial IGenericMessage Saved(ILogger logger, string calculation, string place);

    /// <summary>Logs that a version of a calculation was deleted from a place.</summary>
    [MessageLogging(EventId = 12792, Level = LogLevel.Information, Message = "The version of calculation '{calculation}' at {place} was deleted")]
    public static partial IGenericMessage Deleted(ILogger logger, string calculation, string place);

    /// <summary>Logs a version whose text is not acceptable.</summary>
    [MessageLogging(EventId = 22790, Level = LogLevel.Warning, Message = "The version of calculation '{calculation}' was refused: {reason}")]
    public static partial IGenericMessage VersionInvalid(ILogger logger, string calculation, string reason);

    /// <summary>Logs a calculation that does not exist.</summary>
    [MessageLogging(EventId = 32790, Level = LogLevel.Warning, Message = "There is no calculation '{calculation}'")]
    public static partial IGenericMessage CalculationNotFound(ILogger logger, string calculation);

    /// <summary>Logs a version that does not exist.</summary>
    [MessageLogging(EventId = 32791, Level = LogLevel.Warning, Message = "Calculation '{calculation}' has no version at {place}")]
    public static partial IGenericMessage VersionNotFound(ILogger logger, string calculation, string place);
}
