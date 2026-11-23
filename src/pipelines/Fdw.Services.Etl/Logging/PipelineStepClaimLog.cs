using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the claims of the steps that act outside the run (FDW-6139). A step claimed before that failed, or that never recorded how it ended, is a conflict with what already happened (4); a
/// claim that cannot be written or read is a dependency that failed (7); a host that names no operational store, or registers no claim store for a graph that has such a step, is a configuration that is wrong
/// (6). EventId range: 40920, 40921, 60925, 70923, 70924 and 10926 (information).
/// </summary>
[MessageLoggingTypeCode("PIPESTEPCLAIM")]
public static partial class PipelineStepClaimLog
{
    /// <summary>Logs a step that was claimed and failed before, which a restart does not try again.</summary>
    [MessageLogging(EventId = 40920, Level = LogLevel.Error,
        Message = "Step '{step}' was claimed by run {executionId} and failed there, so it does not act again",
        ClientMessage = "That step acted before and failed; it is not tried a second time")]
    public static partial IGenericMessage ClaimedAndFailed(ILogger logger, string step, Guid executionId);

    /// <summary>Logs a step that was claimed and never recorded how it ended.</summary>
    [MessageLogging(EventId = 40921, Level = LogLevel.Error,
        Message = "Step '{step}' was claimed by run {executionId} and never recorded how it ended; whether its effect happened cannot be known, so it does not act again",
        ClientMessage = "That step may have acted before, and nobody can say how it ended; it is not tried a second time")]
    public static partial IGenericMessage ClaimedWithNoOutcome(ILogger logger, string step, Guid executionId);

    /// <summary>Logs a graph that has a step with an outside effect on a host that registers no claim store, or a run that has no identity to claim under.</summary>
    [MessageLogging(EventId = 60925, Level = LogLevel.Error,
        Message = "Step '{step}' acts outside the run and has to be claimed first, but {missing}",
        ClientMessage = "The host cannot claim a step that acts outside the run")]
    public static partial IGenericMessage CannotClaim(ILogger logger, string step, string missing);

    /// <summary>Logs a claim that could not be written or read.</summary>
    [MessageLogging(EventId = 70923, Level = LogLevel.Error,
        Message = "The claim of step '{step}' could not be made: {reason}",
        ClientMessage = "The claim of the step could not be made")]
    public static partial IGenericMessage NotClaimed(ILogger logger, string step, string? reason);

    /// <summary>Logs the outcome of a step that could not be recorded.</summary>
    [MessageLogging(EventId = 70924, Level = LogLevel.Error,
        Message = "The outcome {outcome} of step '{step}' could not be recorded: {reason}",
        ClientMessage = "How the step ended could not be recorded")]
    public static partial IGenericMessage OutcomeNotRecorded(ILogger logger, string step, string outcome, string? reason);

    /// <summary>Logs a step taken as done because an earlier run of the chain finished it.</summary>
    [MessageLogging(EventId = 10926, Level = LogLevel.Information,
        Message = "Step '{step}' was claimed by run {executionId} and finished there, so it is taken as done and does not act again",
        ClientMessage = "That step already acted and finished")]
    public static partial IGenericMessage TakenAsDone(ILogger logger, string step, Guid executionId);
}
