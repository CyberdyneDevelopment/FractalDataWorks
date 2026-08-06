using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Orchestration.Logging;

/// <summary>
/// Message logging for error handling operations.
/// </summary>
[MessageLoggingTypeCode("ORCHESTRATION")]
public static partial class ErrorHandlingLogger
{
    /// <summary>Logs when a step fails and stops orchestration.</summary>
    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Step '{stepId}' failed: {errorMessage}")]
    public static partial IGenericMessage StepFailedStopOnError(ILogger logger, string stepId, string errorMessage);

    /// <summary>Logs when a step fails but orchestration continues.</summary>
    [MessageLogging(EventId = 91001, Level = LogLevel.Warning, Message = "Skipped failed item in step '{stepId}': {errorMessage}")]
    public static partial IGenericMessage StepFailedSkipAndContinue(ILogger logger, string stepId, string errorMessage);

    /// <summary>Logs when a step attempt fails and will retry.</summary>
    [MessageLogging(EventId = 91002, Level = LogLevel.Warning, Message = "Step '{stepId}' attempt {attemptNumber} failed: {errorMessage}")]
    public static partial IGenericMessage StepAttemptFailed(ILogger logger, string stepId, int attemptNumber, string errorMessage);

    /// <summary>Logs when a failed item is redirected to dead letter.</summary>
    [MessageLogging(EventId = 91003, Level = LogLevel.Warning, Message = "Redirected failed item from step '{stepId}' to dead letter: {errorMessage}")]
    public static partial IGenericMessage RedirectedToDeadLetter(ILogger logger, string stepId, string errorMessage);

    /// <summary>Logs when a step fails and triggers compensation.</summary>
    [MessageLogging(EventId = 91004, Level = LogLevel.Error, Message = "Step '{stepId}' failed, triggering compensation: {errorMessage}")]
    public static partial IGenericMessage StepFailedTriggeringCompensation(ILogger logger, string stepId, string errorMessage);
}
