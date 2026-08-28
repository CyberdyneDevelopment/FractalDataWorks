using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// The evaluation loop every scheduler runs: wake on an interval, evaluate, survive whatever the
/// evaluation threw, and stop cleanly when the host does.
/// </summary>
/// <remarks>
/// The loop is the same wherever a scheduler runs; what it evaluates is not. So the loop is here and
/// <see cref="Evaluate"/> is abstract — a scheduler implementation says what a due schedule is
/// and what to do about it, and inherits the parts that are easy to get subtly wrong.
///
/// Three of those parts are worth naming, because each is a bug when omitted. A cancellation during
/// evaluation is a stop, not a fault, so it breaks the loop rather than being logged as an error. A
/// cancellation during the delay does the same, which is what makes shutdown prompt instead of
/// waiting out the interval. And any other exception is logged and swallowed: an evaluation that
/// throws must not end the loop, or one bad schedule silently stops every schedule.
/// </remarks>
public abstract class SchedulerBackgroundServiceBase : BackgroundService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulerBackgroundServiceBase"/> class.
    /// </summary>
    /// <param name="logger">The logger; <see cref="NullLogger.Instance"/> when DI supplies none.</param>
    protected SchedulerBackgroundServiceBase(ILogger? logger)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Gets how long to wait between evaluations.
    /// </summary>
    /// <remarks>
    /// Read once per loop rather than cached at construction, so a deployment that changes the
    /// interval does not need a restart to take effect.
    /// </remarks>
    protected abstract TimeSpan EvaluationInterval { get; }

    /// <summary>
    /// Evaluates whatever is due and acts on it.
    /// </summary>
    /// <param name="cancellationToken">The host's stopping token.</param>
    /// <returns>A task that completes when this pass is done.</returns>
    protected abstract Task Evaluate(CancellationToken cancellationToken);

    /// <summary>Called once before the first evaluation.</summary>
    /// <param name="interval">The interval the loop will use.</param>
    protected virtual void OnStarted(TimeSpan interval)
    {
    }

    /// <summary>Called once after the loop ends.</summary>
    protected virtual void OnStopped()
    {
    }

    /// <summary>Called when an evaluation throws anything other than cancellation.</summary>
    /// <param name="exception">What it threw.</param>
    protected virtual void OnEvaluationFailed(Exception exception)
    {
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = EvaluationInterval;
        SchedulerLoopLog.LoopStarted(_logger, interval.TotalSeconds);
        OnStarted(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            SchedulerLoopLog.Tick(_logger);
            var started = Stopwatch.GetTimestamp();

            try
            {
                await Evaluate(stoppingToken).ConfigureAwait(false);

                var elapsed = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                SchedulerLoopLog.EvaluationCompleted(_logger, LastEvaluatedCount, elapsed);

                if (elapsed > (long)EvaluationInterval.TotalMilliseconds)
                {
                    SchedulerLoopLog.EvaluationOverran(_logger, elapsed, (long)EvaluationInterval.TotalMilliseconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Why every exception: an evaluation that throws must not end the
            // loop, or one bad schedule silently stops every schedule. The handler reports and
            // continues, which is the only behaviour that keeps the other schedules running.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                OnEvaluationFailed(ex);
                SchedulerLoopLog.EvaluationFailed(_logger, ex);
            }

            try
            {
                await Task.Delay(EvaluationInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        if (!stoppingToken.IsCancellationRequested)
        {
            SchedulerLoopLog.LoopEndedUnexpectedly(_logger);
        }

        SchedulerLoopLog.LoopStopped(_logger);
        OnStopped();
    }

    /// <summary>
    /// Gets how many schedules the last pass looked at, for the completion message.
    /// </summary>
    /// <remarks>
    /// Zero when an implementation does not track it — a count of nothing is honest, and the message
    /// is still worth emitting because it says the pass ran.
    /// </remarks>
    protected virtual int LastEvaluatedCount => 0;
}
