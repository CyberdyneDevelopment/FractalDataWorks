using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling;

/// <summary>
/// MessageLogging for the scheduler evaluation loop.
/// </summary>
/// <remarks>
/// EventId range: 540-546. The severities are the operational reading of each event, not its
/// position in a sequence: a tick is Trace because it happens on every interval forever and is only
/// wanted when tracing a specific window; a pass that evaluated is Debug because it carries a count
/// worth seeing while diagnosing; start and stop are Information because they bracket the service's
/// life; a failed evaluation is Error because that pass did nothing while the loop kept running;
/// and a loop that ends while the host is still up is Critical, because nothing is scheduling any
/// more and no other message would say so.
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class SchedulerLoopLog
{
    /// <summary>The loop began.</summary>
    [MessageLogging(
        EventId = 540,
        Level = LogLevel.Information,
        Message = "Scheduler loop started, evaluating every {intervalSeconds}s")]
    public static partial IGenericMessage LoopStarted(ILogger logger, double intervalSeconds);

    /// <summary>The loop ended because the host is stopping.</summary>
    [MessageLogging(
        EventId = 541,
        Level = LogLevel.Information,
        Message = "Scheduler loop stopped")]
    public static partial IGenericMessage LoopStopped(ILogger logger);

    /// <summary>A tick woke the loop.</summary>
    /// <remarks>
    /// Trace, because this fires on every interval for the life of the process. At any other level
    /// it would bury the messages that matter within minutes.
    /// </remarks>
    [MessageLogging(
        EventId = 542,
        Level = LogLevel.Trace,
        Message = "Scheduler tick")]
    public static partial IGenericMessage Tick(ILogger logger);

    /// <summary>An evaluation pass finished.</summary>
    [MessageLogging(
        EventId = 543,
        Level = LogLevel.Debug,
        Message = "Scheduler evaluated {scheduleCount} schedule(s) in {elapsedMs}ms")]
    public static partial IGenericMessage EvaluationCompleted(ILogger logger, int scheduleCount, long elapsedMs);

    /// <summary>An evaluation threw, and the loop continued.</summary>
    /// <remarks>
    /// Error rather than Critical: this pass did nothing, but the loop is still running and the next
    /// pass may well succeed. Reserving Critical for the loop actually ending is what keeps that
    /// level meaningful.
    /// </remarks>
    [MessageLogging(
        EventId = 544,
        Level = LogLevel.Error,
        Message = "Scheduler evaluation failed; the loop continues")]
    public static partial IGenericMessage EvaluationFailed(ILogger logger, System.Exception ex);

    /// <summary>An evaluation took longer than the interval it runs on.</summary>
    /// <remarks>
    /// Warning, because nothing has failed but the schedule is slipping: passes are now back-to-back
    /// and a due schedule fires late. It is the signal that precedes a real problem.
    /// </remarks>
    [MessageLogging(
        EventId = 545,
        Level = LogLevel.Warning,
        Message = "Scheduler evaluation took {elapsedMs}ms, longer than its {intervalMs}ms interval")]
    public static partial IGenericMessage EvaluationOverran(ILogger logger, long elapsedMs, long intervalMs);

    /// <summary>The loop ended while the host was still running.</summary>
    /// <remarks>
    /// Critical, and the one message here that earns it: nothing is scheduling any more, the process
    /// is otherwise healthy, and no other signal would say so.
    /// </remarks>
    [MessageLogging(
        EventId = 546,
        Level = LogLevel.Critical,
        Message = "Scheduler loop ended while the host is still running; nothing is being scheduled")]
    public static partial IGenericMessage LoopEndedUnexpectedly(ILogger logger);
}
