using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Slots;

/// <summary>
/// A periodic background worker that does its pass only while this copy of an A/B slot app is live (FDW-5846).
/// </summary>
/// <remarks>
/// The copy on the standby slot answers requests and does nothing else to anything shared, so a worker that acts on its own
/// (a timer, a watcher, a poller of a shared store) asks <see cref="ISlotRole"/> before every pass, not once at start: a swap changes a
/// running copy's role. A pass that cannot learn the role does not run, and the reason is reported (the gate's failure carries its code).
/// A pass that throws is reported and the next one runs at the interval. A worker that is fed by a request (a queue a request fills)
/// answers that request on either copy and does not take this base type.
/// </remarks>
public abstract class RoleGatedBackgroundService : BackgroundService
{
    private readonly ISlotRole _role;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RoleGatedBackgroundService"/> class.</summary>
    /// <param name="role">The role of this copy.</param>
    /// <param name="logger">The logger; <see cref="NullLogger.Instance"/> when DI supplies none.</param>
    protected RoleGatedBackgroundService(ISlotRole role, ILogger? logger)
    {
        _role = role;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets how long to wait between passes, read before each wait.</summary>
    protected abstract TimeSpan Interval { get; }

    /// <summary>Does one pass of the work; called only while this copy is live.</summary>
    /// <param name="cancellationToken">The host's stopping token.</param>
    /// <returns>A task that completes when the pass is done.</returns>
    protected abstract Task Pass(CancellationToken cancellationToken);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var worker = GetType().Name;
        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = Interval;
            if (interval <= TimeSpan.Zero)
            {
                SlotRoleLog.IntervalNotPositive(_logger, worker, interval);
                return;
            }

            await RunOnePass(worker, stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunOnePass(string worker, CancellationToken stoppingToken)
    {
        var mayRun = _role.BackgroundWorkMayRun();
        if (mayRun.IsFailure)
        {
            SlotRoleLog.PassSkippedRoleUnknown(_logger, worker);
            return;
        }

        if (mayRun.Value is not true)
        {
            SlotRoleLog.PassSkippedStandby(_logger, worker);
            return;
        }

        try
        {
            await Pass(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping: the pass ended because it was asked to, which is not a failure.
        }
        catch (Exception ex)
        {
            SlotRoleLog.PassFailed(_logger, ex, worker);
        }
    }
}
