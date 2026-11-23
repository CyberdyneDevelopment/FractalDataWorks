using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Logging;

/// <summary>
/// MessageLogging for the recovery of runs carried out at once whose durable claim lapsed (FDW-6986, slice 4 of FDW-6141): a run found half-done ends as its pipeline's half-done rule says, and a
/// run that cannot be judged is left open and reported at every sweep: no rule is assumed for it. EventId range: 60880-60889. These are host and data faults, not a caller's request, so they carry
/// the Configuration category (6).
/// </summary>
[MessageLoggingTypeCode("PIPELINERECOVERY")]
public static partial class PipelineAtOnceRecoveryLog
{
    /// <summary>A run carried out at once lost its claim and ends as its pipeline's half-done rule says (the text is also the reason on the run's record, so it states nothing that differs from one write to the next).</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="pipelineName">Its pipeline.</param>
    /// <param name="rule">The rule its pipeline states.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60880, Level = LogLevel.Warning, Message = "Run {executionId} of pipeline {pipelineName} was half-done, its host having stopped or lost its claim: its pipeline's half-done rule is {rule}, so it ends Interrupted and never runs again")]
    public static partial IGenericMessage RunEndedHalfDone(ILogger logger, Guid executionId, string pipelineName, string rule);

    /// <summary>The pipeline of a run whose claim lapsed cannot be read, so its half-done rule is not known: the run is left open and no rule is assumed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="pipelineName">Its pipeline.</param>
    /// <param name="reason">Why it cannot be read.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60881, Level = LogLevel.Error, Message = "Run {executionId} was half-done (its host stopped or its claim lapsed), but its pipeline {pipelineName} cannot be read ({reason}), so its half-done rule is not known: the run is left open and no rule is assumed")]
    public static partial IGenericMessage PipelineNotReadable(ILogger logger, Guid executionId, string pipelineName, string? reason);

    /// <summary>The pipeline of a run whose claim lapsed states no half-done rule, or one that is not offered: the run is left open and no rule is assumed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="pipelineName">Its pipeline.</param>
    /// <param name="rule">The rule it states, or empty when it states none.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60882, Level = LogLevel.Error, Message = "Run {executionId} was half-done (its host stopped or its claim lapsed), but its pipeline {pipelineName} states the half-done rule '{rule}', which is none of the offered rules: the run is left open and no rule is assumed")]
    public static partial IGenericMessage RuleNotUsable(ILogger logger, Guid executionId, string pipelineName, string rule);

    /// <summary>The record of a run whose claim lapsed cannot be read, so it is not known whether the run was carried out at once or is still open: nothing is closed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="reason">Why it cannot be read.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60883, Level = LogLevel.Error, Message = "Run {executionId} was half-done (its host stopped or its claim lapsed), but its record cannot be read ({reason}), so nothing is closed for it")]
    public static partial IGenericMessage RunRecordNotReadable(ILogger logger, Guid executionId, string? reason);

    /// <summary>The record of a half-done run could not be closed.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="reason">Why.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60884, Level = LogLevel.Error, Message = "Run {executionId} is half-done but its record could not be closed ({reason}); it is tried again at the next sweep")]
    public static partial IGenericMessage RunNotClosed(ILogger logger, Guid executionId, string? reason);

    /// <summary>A sweep for lapsed claims failed, so the runs they would have named are not known yet.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reason">Why.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60885, Level = LogLevel.Error, Message = "The sweep for lapsed claims failed ({reason}); the next sweep asks again")]
    public static partial IGenericMessage SweepFailed(ILogger logger, string? reason);

    /// <summary>The claim procedures handed lapsed claims to the recovery, which judges them at the next settle.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="claims">How many lapsed claim rows were handed over.</param>
    /// <param name="runs">How many different runs they belong to.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60889, Level = LogLevel.Information, Message = "{claims} lapsed claim row(s) of {runs} run(s) were handed to the recovery of half-done runs")]
    public static partial IGenericMessage LapsedClaimsSeen(ILogger logger, int claims, int runs);

    /// <summary>A run the recovery judged has no record, so nothing is closed for it.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The run.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60887, Level = LogLevel.Information, Message = "Run {executionId} was judged by the recovery of half-done runs and has no record, so nothing is closed for it")]
    public static partial IGenericMessage RunHasNoRecord(ILogger logger, Guid executionId);

    /// <summary>A run the recovery judged is not an open run carried out at once, so its own path decides and nothing is closed here.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="isOpen">Whether its execution item has not reached a terminal state.</param>
    /// <param name="runMode">The run mode its record states, or empty when it states none.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60888, Level = LogLevel.Information, Message = "Run {executionId} was judged by the recovery of half-done runs and is not an open run carried out at once (open: {isOpen}, run mode '{runMode}'), so nothing is closed for it")]
    public static partial IGenericMessage RunNotOpenAtOnce(ILogger logger, Guid executionId, bool isOpen, string runMode);

    /// <summary>A run reached its own end after its record was closed by the recovery of half-done runs: its state and reason stand, and the run writes nothing more.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="executionId">The run.</param>
    /// <param name="state">The terminal state its record already has.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 60886, Level = LogLevel.Warning, Message = "Run {executionId} reached its own end, but its record was already closed as {state}: that state and its reason stand, and nothing more is written")]
    public static partial IGenericMessage RunAlreadyClosed(ILogger logger, Guid executionId, string state);
}
