using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for a run that its pipeline holds past the pipeline's limit (FDW-3129): it is held outside the queue until its time, shown with the
/// time it is due, joins the queue when the time comes and is never refused then, and a held run that is cancelled is dropped.
/// </summary>
public sealed class PipelineRunHeldTests
{
    private sealed class ManualTime : TimeProvider
    {
        private readonly List<ManualTimer> _timers = [];
        private DateTimeOffset _now = new(2026, 10, 29, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(callback, state, _now + dueTime);
            _timers.Add(timer);
            return timer;
        }

        public void Advance(TimeSpan by)
        {
            _now += by;
            foreach (var timer in _timers.Where(timer => timer.IsDue(_now)).ToList())
            {
                timer.Fire();
            }
        }
    }

    private sealed class ManualTimer(TimerCallback callback, object? state, DateTimeOffset due) : ITimer
    {
        private bool _done;

        public bool IsDue(DateTimeOffset now) => !_done && due <= now;

        public void Fire()
        {
            _done = true;
            callback(state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();

        public void Dispose() => _done = true;

        public ValueTask DisposeAsync()
        {
            _done = true;
            return ValueTask.CompletedTask;
        }
    }

    private static PipelineRunCap Cap(int limit, int delaySeconds)
    {
        PipelineDelayedStart.TryRead(delaySeconds, null, null, out var delay).ShouldBeEmpty();
        return new PipelineRunCap("pipeline/orders", limit, false, delay);
    }

    private static PipelineRunClaim Claim(PipelineRunIdentity run, PipelineRunCap cap) =>
        PipelineRunClaim.Writing(run, [], new PipelineRunLimitValues(8, 8), cap);

    private static PipelineRunIdentity Run() => new(Guid.NewGuid(), Guid.NewGuid(), "Orders", null);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunPastTheLimitIsHeldShownWithItsTimeThenWaitsItsTurnAndIsNotRefused()
    {
        var time = new ManualTime();
        var scheduler = new PipelineRunScheduler(time);
        var cap = Cap(1, 60);
        var firstPlace = await scheduler.Enter(Claim(Run(), cap), TestContext.Current.CancellationToken);
        var second = Run();

        var place = scheduler.Queue(Claim(second, cap));

        place.IsRefused.ShouldBeFalse();
        var held = scheduler.Snapshot().Waiting.ShouldHaveSingleItem();
        held.Run.ShouldBe(second);
        held.StartedAt.ShouldBeNull();
        var block = held.Waiting.ShouldNotBeNull();
        block.Reason.Name.ShouldBe("HeldUntil");
        block.HeldUntil.ShouldBe(new DateTimeOffset(2026, 10, 29, 9, 1, 0, TimeSpan.Zero));

        time.Advance(TimeSpan.FromSeconds(59));

        scheduler.Snapshot().Waiting.ShouldHaveSingleItem().Waiting.ShouldNotBeNull().Reason.Name.ShouldBe("HeldUntil");

        time.Advance(TimeSpan.FromSeconds(1));

        var queued = scheduler.Snapshot().Waiting.ShouldHaveSingleItem().Waiting.ShouldNotBeNull();
        queued.Reason.Name.ShouldBe("PipelineLimit");
        queued.HeldUntil.ShouldBeNull();

        scheduler.Release(firstPlace);
        await scheduler.Wait(place, TestContext.Current.CancellationToken);

        scheduler.Snapshot().Running.ShouldHaveSingleItem().Run.ShouldBe(second);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunUnderTheLimitStartsAtOnceAndIsNotHeld()
    {
        var scheduler = new PipelineRunScheduler(new ManualTime());
        var cap = Cap(2, 60);
        await scheduler.Enter(Claim(Run(), cap), TestContext.Current.CancellationToken);

        var place = scheduler.Queue(Claim(Run(), cap));
        await scheduler.Wait(place, TestContext.Current.CancellationToken);

        scheduler.Snapshot().Running.Count.ShouldBe(2);
        scheduler.Snapshot().Waiting.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AHeldRunThatIsCancelledIsDroppedAndDoesNotJoinTheQueueLater()
    {
        var time = new ManualTime();
        var scheduler = new PipelineRunScheduler(time);
        var cap = Cap(1, 60);
        await scheduler.Enter(Claim(Run(), cap), TestContext.Current.CancellationToken);
        var place = scheduler.Queue(Claim(Run(), cap));
        using var cancellation = new CancellationTokenSource();
        var waiting = scheduler.Wait(place, cancellation.Token);

        await cancellation.CancelAsync();
        var cancelled = false;
        try
        {
            await waiting;
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }

        cancelled.ShouldBeTrue();
        scheduler.Snapshot().Waiting.ShouldBeEmpty();
        time.Advance(TimeSpan.FromMinutes(5));
        scheduler.Snapshot().Waiting.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunHeldBeforeARestartIsHeldAgainUntilItsRecordedTimeEvenWhenNothingIsInFlight()
    {
        var time = new ManualTime();
        var scheduler = new PipelineRunScheduler(time);
        var run = Run();
        var due = new DateTimeOffset(2026, 10, 29, 9, 30, 0, TimeSpan.Zero);

        var place = scheduler.Queue(Claim(run, Cap(1, 60)), due);

        place.IsRefused.ShouldBeFalse();
        scheduler.Running.ShouldBe(0);
        var held = scheduler.Snapshot().Waiting.ShouldHaveSingleItem();
        held.Run.ShouldBe(run);
        held.Waiting.ShouldNotBeNull().Reason.Name.ShouldBe("HeldUntil");
        held.Waiting.HeldUntil.ShouldBe(due);

        time.Advance(TimeSpan.FromMinutes(29));
        scheduler.Snapshot().Waiting.ShouldHaveSingleItem();
        time.Advance(TimeSpan.FromMinutes(1));
        await scheduler.Wait(place, TestContext.Current.CancellationToken);

        scheduler.Snapshot().Running.ShouldHaveSingleItem().Run.ShouldBe(run);
        scheduler.Snapshot().Waiting.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARecordedTimeThatHasPassedStartsTheRunAtOnceAndItIsNotHeld()
    {
        var scheduler = new PipelineRunScheduler(new ManualTime());
        var run = Run();

        var place = scheduler.Queue(Claim(run, Cap(1, 60)), new DateTimeOffset(2026, 10, 29, 8, 0, 0, TimeSpan.Zero));
        await scheduler.Wait(place, TestContext.Current.CancellationToken);

        place.IsRefused.ShouldBeFalse();
        place.HeldUntil.ShouldBeNull();
        scheduler.Snapshot().Running.ShouldHaveSingleItem().Run.ShouldBe(run);
        scheduler.Snapshot().Waiting.ShouldBeEmpty();
    }
}
