using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.StateCollections;

/// <summary>
/// Logging methods for the smart-state engine.
/// EventId range: 7400-7409 (transitions). Result-code-driven failures use categorized
/// numbers (see StateMachineResultCodes); their messages log via the code itself.
/// </summary>
[MessageLoggingTypeCode("SM")]
public static partial class StateMachineLog
{
    /// <summary>Logs a successful transition.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="from">The state being exited.</param>
    /// <param name="to">The state being entered.</param>
    /// <param name="correlationId">The transition's correlation id.</param>
    /// <returns>A message describing the transition.</returns>
    [MessageLogging(EventId = 11000, Level = LogLevel.Information,
        Message = "State transition {from}→{to} (correlation={correlationId})")]
    public static partial IGenericMessage Transitioned(ILogger logger, string from, string to, string correlationId);

    /// <summary>Logs a transition handler returning a failure result.</summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="handler">The handler type name.</param>
    /// <param name="message">The handler's failure message text.</param>
    /// <returns>A message describing the handler failure.</returns>
    [MessageLogging(EventId = 91000, Level = LogLevel.Warning,
        Message = "Transition handler {handler} returned a failure (transition still committed): {message}")]
    public static partial IGenericMessage HandlerFailed(ILogger logger, string handler, string? message);
}
