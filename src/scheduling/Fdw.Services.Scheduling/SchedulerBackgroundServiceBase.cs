using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
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
///
/// Every evaluation runs in the system context. A scheduler is a service acting as the platform: it
/// reads every tenant's schedules and dispatches them, and nothing about a background loop carries a
/// caller. Without an explicit elevation its connections open as the deny principal, which sees
/// only shared rows -- every tenant-owned schedule silently disappears from the pass. The loop owns
/// this, not each implementation, so no scheduler can forget it; work done FOR one tenant narrows
/// itself (SystemAuthenticationContext(tenantId)) where it dispatches.
/// </remarks>
public abstract class SchedulerBackgroundServiceBase : BackgroundService
{
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly ISlotRole _role;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulerBackgroundServiceBase"/> class.
    /// </summary>
    /// <param name="authContextAccessor">Elevated to the system context for each evaluation.</param>
    /// <param name="role">Says before each pass whether this copy of the app is live; a standby copy evaluates nothing (FDW-5870).</param>
    /// <param name="logger">The logger; <see cref="NullLogger.Instance"/> when DI supplies none.</param>
    protected SchedulerBackgroundServiceBase(IAuthenticationContextAccessor authContextAccessor, ISlotRole role, ILogger? logger)
    {
        _authContextAccessor = authContextAccessor;
        _role = role;
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

            try
            {
                await Pass(stoppingToken).ConfigureAwait(false);
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
    /// One pass: asks whether this copy of the app is live and, only if it is, evaluates (FDW-5870).
    /// </summary>
    /// <remarks>
    /// The role is read on every pass, not once at start: a swap changes a running copy's role. A standby copy answers requests and
    /// does nothing else to anything shared, and a schedule dispatched by both copies would run twice. A role that cannot be read
    /// is not live: the pass does nothing and says so.
    /// </remarks>
    private async Task Pass(CancellationToken stoppingToken)
    {
        var mayRun = _role.BackgroundWorkMayRun();
        if (mayRun.IsFailure)
        {
            SchedulerLoopLog.PassSkippedRoleUnknown(_logger);
            return;
        }

        if (mayRun.Value is not true)
        {
            SchedulerLoopLog.PassSkippedStandby(_logger);
            return;
        }

        var started = Stopwatch.GetTimestamp();

        // Per pass, not around the whole loop: the scope restores the previous context when
        // the pass ends, so nothing outside an evaluation -- the delay, the logging -- runs
        // elevated.
        using (new SystemAuthenticationContextScope(_authContextAccessor))
        {
            await Evaluate(stoppingToken).ConfigureAwait(false);
        }

        var elapsed = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        SchedulerLoopLog.EvaluationCompleted(_logger, LastEvaluatedCount, elapsed);

        if (elapsed > (long)EvaluationInterval.TotalMilliseconds)
        {
            SchedulerLoopLog.EvaluationOverran(_logger, elapsed, (long)EvaluationInterval.TotalMilliseconds);
        }
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
