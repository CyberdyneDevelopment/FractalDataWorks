using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Holds the runs the AtOnce runner started, for the whole host (FDW-6141): the runner is scoped to a request, but a run outlives the request that started it, so this
/// singleton keeps the runs, hands them the token the host's stop cancels, and waits for them to end before the host is gone.
/// </summary>
/// <remarks>
/// A run that is cut off by the host's stop is closed by the shared run code as Interrupted (it keeps its stored request), exactly as a queued run is; this class only gives it
/// the token and the time to do so, within the time the host allows its services to stop.
/// </remarks>
public sealed class PipelineAtOnceRuns : IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<Task, byte> _running = new();
    private readonly ConcurrentDictionary<Guid, PipelineRunTimeAndStepLimits> _limits = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly ILogger<PipelineAtOnceRuns> _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineAtOnceRuns"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public PipelineAtOnceRuns(ILogger<PipelineAtOnceRuns>? logger = null)
    {
        _logger = logger ?? NullLogger<PipelineAtOnceRuns>.Instance;
    }

    /// <summary>Gets the token the host's stop cancels, which every run the runner starts follows.</summary>
    internal CancellationToken Stopping => _stopping.Token;

    /// <summary>Holds the limits of an at-once run for as long as it runs (FDW-6701), so a pipeline it starts can be bounded by what is left of them.</summary>
    /// <param name="executionId">The execution of the run.</param>
    /// <param name="limits">The run's time and step limits.</param>
    /// <returns>What ends the hold: disposed when the run ends.</returns>
    internal IDisposable HoldLimits(Guid executionId, PipelineRunTimeAndStepLimits limits)
    {
        _limits[executionId] = limits;
        return new LimitsHold(_limits, executionId);
    }

    /// <summary>Finds the limits of an at-once run held now.</summary>
    /// <param name="executionId">The execution of the run.</param>
    /// <returns>The run's limits; <see langword="null"/> when the host holds no at-once run of that execution (a queued run, a run that has ended, or one of another host).</returns>
    internal PipelineRunTimeAndStepLimits? LimitsOf(Guid executionId) =>
        _limits.TryGetValue(executionId, out var limits) ? limits : null;

    /// <summary>Starts a run on a task of its own and holds it until it ends.</summary>
    /// <param name="run">The run.</param>
    internal void Run(Func<Task> run)
    {
        var task = Task.Run(async () =>
        {
            try
            {
                await run().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _ = PipelineRunLog.AtOnceRunEndedWithException(_logger, ex);
            }
        }, CancellationToken.None);
        _running[task] = 0;
        _ = task.ContinueWith(ended => _running.TryRemove(ended, out _), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>Waits for the runs held now to end.</summary>
    /// <param name="patience">The longest to wait; <see cref="Timeout.InfiniteTimeSpan"/> waits as long as the token allows.</param>
    /// <param name="cancellationToken">Ends the wait.</param>
    /// <returns><see langword="true"/> when every run held now has ended; <see langword="false"/> when the time ran out or the token ended the wait first.</returns>
    internal async Task<bool> Drain(TimeSpan patience, CancellationToken cancellationToken = default)
    {
        using var cutoff = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ended = Task.WhenAll(_running.Keys.ToArray());
        var first = await Task.WhenAny(ended, Task.Delay(patience, cutoff.Token)).ConfigureAwait(false);
        await cutoff.CancelAsync().ConfigureAwait(false);
        return ReferenceEquals(first, ended);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _stopping.CancelAsync().ConfigureAwait(false);

        // The host's time to stop bounds the wait: a run still running when it ends is left open with its claim, which lapses, and the next start's sweep ends it as its half-done rule says.
        var inFlight = _running.Count;
        var allEnded = await Drain(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        _ = PipelineRunRecoveryLog.AtOnceDrainFinished(_logger, inFlight, allEnded);
    }

    /// <inheritdoc />
    public void Dispose() => _stopping.Dispose();

    private sealed class LimitsHold(ConcurrentDictionary<Guid, PipelineRunTimeAndStepLimits> held, Guid executionId) : IDisposable
    {
        public void Dispose() => _ = held.TryRemove(executionId, out _);
    }
}
