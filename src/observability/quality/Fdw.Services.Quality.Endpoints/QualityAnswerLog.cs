using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>The coded messages the quality endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("QUALITYANSWERS")]
public static partial class QualityAnswerLog
{

    /// <summary>Logs that Quality rule was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Quality rule was not found, or is not visible to the caller", ClientMessage = "Quality rule was not found")]
    public static partial IGenericMessage GetQualityRuleQualityruleNotFound(ILogger logger);

    /// <summary>Logs that Quality check was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "Quality check was not found, or is not visible to the caller", ClientMessage = "Quality check was not found")]
    public static partial IGenericMessage ExecuteQualityCheckQualitycheckNotFound(ILogger logger);

    /// <summary>Logs that Quality rule was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30003, Level = LogLevel.Warning, Message = "Quality rule was not found, or is not visible to the caller", ClientMessage = "Quality rule was not found")]
    public static partial IGenericMessage UpdateQualityRuleQualityruleNotFound(ILogger logger);

    /// <summary>Logs that the request failed: DataSetName is required.</summary>
    [MessageLogging(EventId = 20001, Level = LogLevel.Warning, Message = "DataSetName is required", ClientMessage = "DataSetName is required")]
    public static partial IGenericMessage ExecuteAllQualityChecksDatasetnameIsRequired(ILogger logger);
}
