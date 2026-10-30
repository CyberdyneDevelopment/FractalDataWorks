using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Decides which pipeline runs are in flight at once (FDW-3085). It replaces the one-permit slot of FDW-1511.
/// </summary>
/// <remarks>
/// <para>
/// A run may start when fewer runs than its server limit are in flight, fewer runs of its tenant than its tenant limit are in flight,
/// fewer runs of its own pipeline than that pipeline's limit are in flight (FDW-3117), and no run in flight writes a table it writes. A run that must run alone starts only when nothing is in flight, and while it is in flight
/// nothing else starts. A run takes every table it writes at once, so two runs can never each hold a table the other waits for.
/// </para>
/// <para>
/// Waiting runs are tried in the order they arrived each time a place frees. A later run may start while an earlier one waits, unless it
/// writes a table the earlier one writes, is a run of the same pipeline, or the earlier one must run alone: that keeps the order for the
/// runs that share a table or a pipeline and stops a run that needs the whole host from being passed for ever.
/// </para>
/// <para>
/// A run whose pipeline refuses a run past its limit is not queued when its pipeline already has that many runs in flight or waiting: its
/// place is returned refused, and it never starts.
/// </para>
/// <para>
/// A run whose pipeline holds a run past its limit (FDW-3129) is not queued when its pipeline already has that many runs in flight or
/// waiting: it is held outside the list until its delay has passed or its time of day comes, and then joins the list like any other run. It
/// is never refused when its time comes; it waits its turn.
/// </para>
/// <para>
/// A run that waits for the pipeline it started hands its place back while it waits, so that pipeline can run, and takes it again
/// afterwards. Only a run that holds a place has one to hand back; the place is found from the flow of the run itself.
/// </para>
/// </remarks>
public sealed class PipelineRunScheduler
{
    private readonly Lock _gate = new();
    private readonly List<PipelineRunPlace> _running = [];
    private readonly List<PipelineRunPlace> _waiting = [];
    private readonly List<PipelineRunPlace> _delayed = [];
    private readonly AsyncLocal<PipelineRunPlace?> _held = new();
    private readonly TimeProvider _time;
    private readonly ILogger _logger;
    private readonly IPipelineWaitObserver? _observer;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunScheduler"/> class.</summary>
    /// <param name="time">The clock the queued and started times are read from.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="observer">Told each time the runs that wait change (FDW-3161); none when nothing watches the waiting runs.</param>
    public PipelineRunScheduler(TimeProvider time, ILogger<PipelineRunScheduler>? logger = null, IPipelineWaitObserver? observer = null)
    {
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _logger = logger ?? NullLogger<PipelineRunScheduler>.Instance;
        _observer = observer;
    }

    /// <summary>Gets how many runs are in flight.</summary>
    public int Running
    {
        get
        {
            lock (_gate)
            {
                return _running.Count;
            }
        }
    }

    /// <summary>Gets how many runs wait for their turn.</summary>
    public int Waiting
    {
        get
        {
            lock (_gate)
            {
                return _waiting.Count;
            }
        }
    }

    /// <summary>Queues a run, in the order it arrived, without waiting for its turn.</summary>
    /// <param name="claim">What the run needs.</param>
    /// <param name="heldUntil">When a run that was held before a host restart is due: it is held again until then, and starts at once when that time has passed (FDW-3216); none for a run that arrives now.</param>
    /// <returns>The run's place, which <see cref="Wait"/> completes once the run may start; a place that is refused (<see cref="PipelineRunPlace.IsRefused"/>) is not queued and never starts.</returns>
    public PipelineRunPlace Queue(PipelineRunClaim claim, DateTimeOffset? heldUntil = null)
    {
        ArgumentNullException.ThrowIfNull(claim);
        if (claim.ServerLimit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(claim), claim.ServerLimit, "A claim's server limit is at least one.");
        }

        if (claim.TenantLimit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(claim), claim.TenantLimit, "A claim's tenant limit is at least one.");
        }

        var place = new PipelineRunPlace(claim) { QueuedAt = _time.GetUtcNow() };
        lock (_gate)
        {
            Admit(place, claim, heldUntil);
        }

        Changed();
        return place;
    }

    // Called with the gate held. Where a run that arrives goes: held for the time it is due, refused, held past its pipeline's limit, or into the list.
    private void Admit(PipelineRunPlace place, PipelineRunClaim claim, DateTimeOffset? heldUntil)
    {
        if (heldUntil is { } due)
        {
            HoldUntil(place, due);
        }
        else if (claim.Cap is { RefusesWhenFull: true } cap && InFlightOf(cap) >= cap.Limit)
        {
            place.RefusedLimit = cap.Limit;
        }
        else if (claim.Cap is { Delay: { } delay } held && InFlightOf(held) >= held.Limit)
        {
            HoldBack(place, held, delay);
        }
        else
        {
            Enlist(place);
        }
    }

    // Tells the observer the runs that wait changed. Called after the gate is let go, because the observer reads the list.
    private void Changed() => _observer?.WaitingChanged(Snapshot());

    /// <summary>Waits for a queued run's turn.</summary>
    /// <param name="place">The place <see cref="Queue"/> returned.</param>
    /// <param name="cancellationToken">Cancels the wait and takes the run out of the list; a run that has started is not affected.</param>
    /// <returns>A task that ends when the run may start.</returns>
    public async Task Wait(PipelineRunPlace place, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(place);
        if (place.IsRefused)
        {
            throw new InvalidOperationException("A refused run is never queued, so it has no turn to wait for.");
        }

        using var registration = cancellationToken.Register(() => Withdraw(place, cancellationToken));
        await place.WaitForTurn().ConfigureAwait(false);
    }

    /// <summary>Queues a run and waits for its turn.</summary>
    /// <param name="claim">What the run needs.</param>
    /// <param name="cancellationToken">Cancels the wait; a run that has started is not affected.</param>
    /// <returns>The run's place, once it may start.</returns>
    public async Task<PipelineRunPlace> Enter(PipelineRunClaim claim, CancellationToken cancellationToken = default)
    {
        var place = Queue(claim);
        await Wait(place, cancellationToken).ConfigureAwait(false);
        return place;
    }

    /// <summary>Lists the runs in flight and the runs waiting, with the reason each waiting run cannot start now (FDW-3097).</summary>
    /// <returns>The list as it stood at one instant; later changes do not alter it.</returns>
    public PipelineRunQueueSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new PipelineRunQueueSnapshot(
                [.. _running.Select(place => Entry(place, null))],
                [.. _waiting.Select((place, index) => Entry(place, BlockerOf(place, index))), .. _delayed.Select(place => Entry(place, HeldBlock(place)))]);
        }
    }

    /// <summary>Says that the run in the current flow holds a place; the worker calls it on the task that runs the run.</summary>
    /// <param name="place">The place the run holds.</param>
    public void Hold(PipelineRunPlace place)
    {
        ArgumentNullException.ThrowIfNull(place);
        _held.Value = place;
    }

    /// <summary>Gives a place back when the run ends.</summary>
    /// <param name="place">The place the run holds.</param>
    public void Release(PipelineRunPlace place)
    {
        ArgumentNullException.ThrowIfNull(place);
        _held.Value = null;
        lock (_gate)
        {
            _running.Remove(place);
            _waiting.Remove(place);
            Dispatch();
        }

        Changed();
    }

    /// <summary>Hands the place of the run in the current flow back for as long as the returned handle lives.</summary>
    /// <returns>A handle that takes the place again when disposed; one that does nothing when the current flow holds no place.</returns>
    public IAsyncDisposable Yield()
    {
        var place = _held.Value;
        if (place is null)
        {
            return NothingToGiveBack.Instance;
        }

        lock (_gate)
        {
            if (!_running.Remove(place))
            {
                return NothingToGiveBack.Instance;
            }

            Dispatch();
        }

        Changed();
        PipelineRunSchedulerLog.PlaceHandedBack(_logger);
        return new TakeAgain(this, place);
    }

    // Called with the gate held. The runs of one pipeline that are running or waiting, which is what its limit counts.
    private int InFlightOf(PipelineRunCap cap) =>
        _running.Count(place => place.Claim.Cap?.Key == cap.Key) + _waiting.Count(place => place.Claim.Cap?.Key == cap.Key);

    private void Enlist(PipelineRunPlace place)
    {
        lock (_gate)
        {
            place.Started = PipelineRunPlace.NewStarted();
            Join(place);
        }
    }

    // Called with the gate held. Puts a run in the list in the order it joins and starts what may start. A held run keeps the turn its caller
    // already waits on, so it joins without a new one.
    private void Join(PipelineRunPlace place)
    {
        _waiting.Add(place);
        Dispatch();
        if (_waiting.Contains(place))
        {
            PipelineRunSchedulerLog.RunWaits(_logger, _running.Count, _waiting.Count);
        }
    }

    // Called with the gate held. Holds a run past its pipeline's limit until its delay has passed or its time of day comes.
    private void HoldBack(PipelineRunPlace place, PipelineRunCap cap, PipelineDelayedStart delay)
    {
        var now = _time.GetUtcNow();
        var due = delay.DueAt(now);
        place.Started = PipelineRunPlace.NewStarted();
        place.HeldUntil = due;
        _delayed.Add(place);
        place.HoldTimer = _time.CreateTimer(_ => HeldRunDue(place), null, due - now, Timeout.InfiniteTimeSpan);
        PipelineRunSchedulerLog.RunHeld(_logger, place.Claim.Run.PipelineName, cap.Limit, due, delay.Description);
    }

    // Called with the gate held. Puts a run that was held before a host restart back in the held list for the time it was held until; a time that has
    // passed means the run is due, and it joins the list like a held run whose time comes (FDW-3216).
    private void HoldUntil(PipelineRunPlace place, DateTimeOffset due)
    {
        var now = _time.GetUtcNow();
        place.Started = PipelineRunPlace.NewStarted();
        if (due <= now)
        {
            Join(place);
            return;
        }

        place.HeldUntil = due;
        _delayed.Add(place);
        place.HoldTimer = _time.CreateTimer(_ => HeldRunDue(place), null, due - now, Timeout.InfiniteTimeSpan);
        PipelineRunSchedulerLog.RunHeldAgain(_logger, place.Claim.Run.PipelineName, due);
    }

    // Fires on the timer of a held run. The run joins the list like a run that has just arrived and is not refused, whatever its pipeline has in
    // flight then. A run withdrawn before its time is no longer held, and nothing happens.
    private void HeldRunDue(PipelineRunPlace place)
    {
        lock (_gate)
        {
            if (!_delayed.Remove(place))
            {
                return;
            }

            StopHold(place);
            PipelineRunSchedulerLog.HeldRunDue(_logger, place.Claim.Run.PipelineName);
            Join(place);
        }

        Changed();
    }

    private static void StopHold(PipelineRunPlace place)
    {
        place.HoldTimer?.Dispose();
        place.HoldTimer = null;
        place.HeldUntil = null;
    }

    private static PipelineRunBlock HeldBlock(PipelineRunPlace place) => new(RunWaitReasons.HeldUntil, null, null, null, place.HeldUntil);

    private void Withdraw(PipelineRunPlace place, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_waiting.Remove(place))
            {
                place.Started.TrySetCanceled(cancellationToken);
                Dispatch();
            }
            else if (_delayed.Remove(place))
            {
                StopHold(place);
                place.Started.TrySetCanceled(cancellationToken);
            }
        }

        Changed();
    }

    // Called with the gate held. Starts every waiting run that may start now, in the order they arrived.
    private void Dispatch()
    {
        var index = 0;
        while (index < _waiting.Count)
        {
            var candidate = _waiting[index];
            if (BlockerOf(candidate, index) is null)
            {
                _waiting.RemoveAt(index);
                _running.Add(candidate);
                if (candidate.StartedAt is null)
                {
                    candidate.StartedAt = _time.GetUtcNow();
                }

                PipelineRunSchedulerLog.RunStarts(_logger, _running.Count, _waiting.Count);
                candidate.Started.TrySetResult();
            }
            else
            {
                index++;
            }
        }
    }

    // Called with the gate held. Why a waiting run cannot start now, or null when it may. Dispatch starts a run exactly when this is null and
    // Snapshot reports what it returns, so the list and the dispatch cannot disagree.
    private PipelineRunBlock? BlockerOf(PipelineRunPlace candidate, int index)
    {
        var claim = candidate.Claim;
        if (claim.Alone)
        {
            return _running.Count == 0 && index == 0
                ? null
                : new PipelineRunBlock(RunWaitReasons.NeedsHostAlone, null, _running.Concat(_waiting.Take(index)).First().Claim.Run.ExecutionId, null);
        }

        if (_running.Count >= claim.ServerLimit)
        {
            return new PipelineRunBlock(RunWaitReasons.ServerLimit, claim.ServerLimit, null, null);
        }

        if (_running.Count(place => place.Claim.TenantId == claim.TenantId) >= claim.TenantLimit)
        {
            return new PipelineRunBlock(RunWaitReasons.TenantLimit, claim.TenantLimit, null, null);
        }

        if (claim.Cap is { } cap && _running.Count(place => place.Claim.Cap?.Key == cap.Key) >= cap.Limit)
        {
            return new PipelineRunBlock(RunWaitReasons.PipelineLimit, cap.Limit, null, null);
        }

        if (_running.FirstOrDefault(place => place.Claim.Alone) is { } alone)
        {
            return new PipelineRunBlock(RunWaitReasons.RunningAlone, null, alone.Claim.Run.ExecutionId, null);
        }

        if (_running.FirstOrDefault(place => place.Claim.WriteKeys.Overlaps(claim.WriteKeys)) is { } sharing)
        {
            return new PipelineRunBlock(
                RunWaitReasons.SharedTable, null, sharing.Claim.Run.ExecutionId, sharing.Claim.WriteKeys.First(key => claim.WriteKeys.Contains(key)));
        }

        return _waiting.Take(index).FirstOrDefault(earlier => earlier.Claim.Alone || earlier.Claim.WriteKeys.Overlaps(claim.WriteKeys) || SamePipeline(earlier, claim)) is { } ahead
            ? new PipelineRunBlock(RunWaitReasons.BehindEarlierRun, null, ahead.Claim.Run.ExecutionId, null)
            : null;
    }

    private static PipelineRunQueueEntry Entry(PipelineRunPlace place, PipelineRunBlock? waiting) =>
        new(place.Claim.Run, place.QueuedAt, place.StartedAt, waiting);

    private static bool SamePipeline(PipelineRunPlace earlier, PipelineRunClaim claim) =>
        claim.Cap is { } cap && earlier.Claim.Cap?.Key == cap.Key;

    private async Task Reenter(PipelineRunPlace place)
    {
        Enlist(place);
        Changed();

        // Not cancellable: the run ends by releasing its place, so it has to hold it again first.
        await place.WaitForTurn().ConfigureAwait(false);
        PipelineRunSchedulerLog.PlaceTakenAgain(_logger);
    }

    private sealed class NothingToGiveBack : IAsyncDisposable
    {
        internal static readonly NothingToGiveBack Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TakeAgain(PipelineRunScheduler owner, PipelineRunPlace place) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await owner.Reenter(place).ConfigureAwait(false);
    }
}
