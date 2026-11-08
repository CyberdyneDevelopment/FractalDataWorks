using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>The coded messages the scheduling endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("SCHEDANSWERS")]
public static partial class SchedulingAnswerLog
{

    /// <summary>Logs that Toggle schedule was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "Toggle schedule was not found, or is not visible to the caller", ClientMessage = "Toggle schedule was not found")]
    public static partial IGenericMessage ToggleScheduleTogglescheduleNotFound(ILogger logger);
}
