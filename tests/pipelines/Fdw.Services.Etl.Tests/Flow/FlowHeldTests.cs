using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for what a component holds in memory (FDW-5231): the gauge adds what it holds and keeps its peak, a pump holds what it buffers until the component reads
/// it, and the live view reads a started component's held figures.
/// </summary>
public sealed class FlowHeldTests
{
    private static FlowBatch Batch(long bytes, params string[] values) =>
        new([.. values.Select(value => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Name"] = value })], bytes);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheGaugeHoldsAndReleasesBatchesAndKeepsItsPeak()
    {
        var gauge = new FlowHeldGauge();

        gauge.Hold(Batch(100, "a", "b"));
        gauge.Hold(Batch(50, "c"));
        gauge.Release(Batch(100, "a", "b"));

        gauge.Count.ShouldBe(new FlowHeldCount(50, 1, 150));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ReleasingEverythingKeepsThePeak()
    {
        var gauge = new FlowHeldGauge();
        gauge.Hold(Batch(80, "a"));

        gauge.ReleaseAll();

        gauge.Count.ShouldBe(new FlowHeldCount(0, 0, 80));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ABatchNoOutputStampedIsMeasured()
    {
        var gauge = new FlowHeldGauge();
        var unstamped = new FlowBatch([new Dictionary<string, object?> { ["Name"] = "abcd" }]);

        gauge.Hold(unstamped);

        gauge.Count.Bytes.ShouldBe(RowSize.Of(unstamped));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task APumpedBatchIsHeldUntilTheComponentReadsIt()
    {
        var state = new FlowInputState("input", 4, 1);
        state.Queue(40);
        state.Queue(60);
        await state.Writer.WriteAsync(Batch(40, "a"), TestContext.Current.CancellationToken);
        await state.Writer.WriteAsync(Batch(60, "b"), TestContext.Current.CancellationToken);
        state.LinkFinished();
        var gauge = new FlowHeldGauge();

        var pump = FlowInputPump.Start(new FlowChannelInput(state), gauge, TestContext.Current.CancellationToken);
        await pump.Completion;

        gauge.Count.Bytes.ShouldBe(100, "both batches wait in the pump's buffer");
        var read = new List<FlowBatch>();
        await foreach (var batch in pump.ReadAll(TestContext.Current.CancellationToken))
        {
            read.Add(batch);
        }

        read.Count.ShouldBe(2);
        state.QueuedBytes.ShouldBe(0, "the pump took both batches off the input's queue");
        gauge.Count.ShouldBe(new FlowHeldCount(0, 0, 100));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheLiveViewReadsWhatAStartedComponentHoldsAndNothingBeforeItStarts()
    {
        var running = new RunningFlows();
        var runId = Guid.NewGuid();
        using var live = running.Begin(runId);
        using var graph = live.Open(
            [new FlowLiveComponent("sort", 1, true)],
            () => [],
            name => string.Equals(name, "sort", StringComparison.OrdinalIgnoreCase) ? new FlowHeldCount(300, 3, 500) : new FlowHeldCount(0, 0, 0));

        running.Components(runId).ShouldNotBeNull().ShouldHaveSingleItem().HeldBytes.ShouldBeNull("a component that has not started holds nothing yet, not zero");

        graph.ComponentStarted("sort");
        var sort = running.Components(runId).ShouldNotBeNull().ShouldHaveSingleItem();

        sort.HeldBytes.ShouldBe(300);
        sort.HeldRows.ShouldBe(3);
        sort.PeakHeldBytes.ShouldBe(500);
    }
}
