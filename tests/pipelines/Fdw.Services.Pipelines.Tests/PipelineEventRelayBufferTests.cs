using System;
using System.Linq;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// The events a host that serves no hub holds between sends (FDW-5249): they leave in the order they came, a run's components update is kept
/// once with its latest figures, and no other kind is merged or dropped.
/// </summary>
[Trait("Category", "CoreFramework")]
public sealed class PipelineEventRelayBufferTests
{
    private static readonly Guid RunA = new("aaaaaaaa-0000-0000-0000-00000000000a");
    private static readonly Guid RunB = new("aaaaaaaa-0000-0000-0000-00000000000b");

    private static PipelineEventRequest Components(Guid run, int rows)
        => new()
        {
            Kind = "ComponentsChanged",
            Components = new PipelineComponentsUpdate { PipelineName = "Nightly", ExecutionId = run, Components = [new() { Name = "src", Rows = rows }] },
        };

    private static PipelineEventRequest Status(Guid run, string status)
        => new() { Kind = "StatusChanged", Status = new PipelineStatusUpdate { PipelineName = "Nightly", ExecutionId = run, Status = status } };

    [Fact]
    [Trait("Priority", "P1")]
    public void EventsLeaveInTheOrderTheyCameAndTheBufferIsThenEmpty()
    {
        var buffer = new PipelineEventRelayBuffer(10);
        var first = Status(RunA, "Running");
        var second = Components(RunA, 1);
        var third = Status(RunA, "Completed");

        buffer.Add(first);
        buffer.Add(second);
        buffer.Add(third);

        buffer.Drain().ShouldBe([first, second, third]);
        buffer.Count.ShouldBe(0);
        buffer.Drain().ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ANewComponentsUpdateOfTheSameRunReplacesTheWaitingOneAtTheLaterPlace()
    {
        var buffer = new PipelineEventRelayBuffer(10);
        var status = Status(RunA, "Running");
        var older = Components(RunA, 1);
        var newer = Components(RunA, 2);

        buffer.Add(older);
        buffer.Add(status);
        buffer.Add(newer);

        buffer.Drain().ShouldBe([status, newer]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ComponentsUpdatesOfDifferentRunsAreNotMerged()
    {
        var buffer = new PipelineEventRelayBuffer(10);
        var a = Components(RunA, 1);
        var b = Components(RunB, 1);

        buffer.Add(a);
        buffer.Add(b);

        buffer.Drain().ShouldBe([a, b]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void EventsOfEveryOtherKindAreKeptAsTheyCameEvenWhenTheyRepeat()
    {
        var buffer = new PipelineEventRelayBuffer(10);
        var events = new[]
        {
            Status(RunA, "Running"),
            Status(RunA, "Running"),
            new PipelineEventRequest { Kind = "Paused", ExecutionId = RunA },
            new PipelineEventRequest { Kind = "Resumed", ExecutionId = RunA },
        };

        foreach (var e in events)
        {
            buffer.Add(e);
        }

        buffer.Drain().ShouldBe(events);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void OverItsCapacityTheOldestComponentsUpdateIsDroppedWithACodedWarning()
    {
        var logger = new RelayRecordingLogger<PipelineEventRelayBuffer>();
        var buffer = new PipelineEventRelayBuffer(3, logger);
        var oldest = Components(RunA, 1);
        var status = Status(RunA, "Running");
        var other = Components(RunB, 1);
        var extra = Status(RunB, "Running");

        buffer.Add(oldest);
        buffer.Add(status);
        buffer.Add(other);
        buffer.Add(extra);

        buffer.Drain().ShouldBe([status, other, extra]);
        logger.EventIds.ShouldBe([60314]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void StatusEventsAreAdmittedBeyondTheCapacityNeverDropped()
    {
        var logger = new RelayRecordingLogger<PipelineEventRelayBuffer>();
        var buffer = new PipelineEventRelayBuffer(2, logger);
        var events = Enumerable.Range(0, 5).Select(i => Status(RunA, "S" + i)).ToList();

        foreach (var e in events)
        {
            buffer.Add(e);
        }

        buffer.Drain().ShouldBe(events);
        logger.EventIds.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ACapacityBelowOneIsRefused()
        => Should.Throw<ArgumentOutOfRangeException>(() => new PipelineEventRelayBuffer(0));

    [Fact]
    [Trait("Priority", "P1")]
    public void TheHubDeclaresTheRelayTuningAsTheDefaultsTheTuningReads()
    {
        var declared = new PipelineHubOption().DeclaredDefaults;
        declared[PipelineHubOption.RelayFlushMilliseconds].ShouldBe(500);
        declared[PipelineHubOption.RelayBufferCapacity].ShouldBe(1000);

        var tuning = PipelineEventRelayTuning.Read(new RelayRecordingLogger<PipelineEventRelayTuning>());

        tuning.FlushEvery.ShouldBe(TimeSpan.FromMilliseconds(500));
        tuning.BufferCapacity.ShouldBe(1000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ATuningThatIsNotPositiveIsRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new PipelineEventRelayTuning(TimeSpan.Zero, 1));
        Should.Throw<ArgumentOutOfRangeException>(() => new PipelineEventRelayTuning(TimeSpan.FromSeconds(1), 0));
    }
}
