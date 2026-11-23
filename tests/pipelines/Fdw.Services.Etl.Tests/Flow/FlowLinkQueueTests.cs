using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for what waits in a link now (FDW-5230): a batch carries its estimated value bytes from the output that wrote it, the input it waits in counts it until the
/// reader takes it, and a writer that waits on a full input records the wait.
/// </summary>
public sealed class FlowLinkQueueTests
{
    private static FlowBatch Batch(params string[] values) =>
        new([.. values.Select(value => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Name"] = value })]);

    private static (FlowChannelOutput Output, FlowLinkState Link, FlowInputState Input) OneLink(int capacity)
    {
        var input = new FlowInputState("input", capacity, 1);
        var link = new FlowLinkState(new FlowLink("src", "output", "dst", "input"), input);
        return (new FlowChannelOutput("output", [link], "src", null, null), link, input);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AWrittenBatchIsQueuedWithItsBytesUntilTheReaderTakesIt()
    {
        var (output, link, input) = OneLink(capacity: 4);
        var batch = Batch("ab", "cde");

        await output.Write(batch, TestContext.Current.CancellationToken);

        link.Count.QueuedBytes.ShouldBe(RowSize.Of(batch));
        link.Count.QueuedBatches.ShouldBe(1);

        var read = await new FlowChannelInput(input).Read(TestContext.Current.CancellationToken);

        read.ShouldNotBeNull().Bytes.ShouldBe(RowSize.Of(batch), "the batch carries the size its output stamped");
        link.Count.QueuedBytes.ShouldBe(0);
        link.Count.QueuedBatches.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AFanOutQueuesTheBatchOnceInEachInputWithTheSameBytes()
    {
        var first = new FlowInputState("input", 4, 1);
        var second = new FlowInputState("input", 4, 1);
        var toFirst = new FlowLinkState(new FlowLink("src", "output", "a", "input"), first);
        var toSecond = new FlowLinkState(new FlowLink("src", "output", "b", "input"), second);
        var batch = Batch("abcd");

        await new FlowChannelOutput("output", [toFirst, toSecond], "src", null, null).Write(batch, TestContext.Current.CancellationToken);

        toFirst.Count.QueuedBytes.ShouldBe(RowSize.Of(batch));
        toSecond.Count.QueuedBytes.ShouldBe(RowSize.Of(batch));
        (await new FlowChannelInput(second).Read(TestContext.Current.CancellationToken)).ShouldNotBeNull().Bytes.ShouldBe(RowSize.Of(batch), "a copy keeps the stamp");
        toSecond.Count.QueuedBatches.ShouldBe(0);
        toFirst.Count.QueuedBatches.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AWriteThatWaitsOnAFullInputRecordsTheWaitAndOneThatDoesNotRecordsNone()
    {
        var (output, link, input) = OneLink(capacity: 1);

        await output.Write(Batch("a"), TestContext.Current.CancellationToken);
        link.Count.Blocked.ShouldBe(TimeSpan.Zero, "the first batch found room at once");

        var waiting = output.Write(Batch("b"), TestContext.Current.CancellationToken);
        waiting.IsCompleted.ShouldBeFalse("the input holds one batch and is full");

        _ = await new FlowChannelInput(input).Read(TestContext.Current.CancellationToken);
        await waiting;

        link.Count.Blocked.ShouldBeGreaterThan(TimeSpan.Zero);
        link.Count.QueuedBatches.ShouldBe(1, "the second batch is waiting now");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AWriteThatIsCancelledLeavesNothingQueued()
    {
        var (output, link, _) = OneLink(capacity: 1);
        await output.Write(Batch("a"), TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => output.Write(Batch("b"), cancelled.Token));

        link.Count.QueuedBatches.ShouldBe(1, "only the batch that was handed over is waiting");
        link.Count.QueuedBytes.ShouldBe(RowSize.Of(Batch("a")));
    }
}
