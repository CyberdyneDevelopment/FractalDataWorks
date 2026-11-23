using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the gate the reads feeding a join across data stores wait at (FDW-6970): no more of them run at once than the strategy's degree, the build
/// side starts first when the strategy orders reads, and a join under the gate gives the same rows as without it.
/// </summary>
public sealed class FlowReadGateTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADegreeOfOneLetsOneReadRunAtATime()
    {
        var gate = new FlowReadGate(new FlowReadGateSettings(1, false), 0);
        var running = 0;
        var most = 0;

        await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
        {
            var slot = await gate.Enter(false, TestContext.Current.CancellationToken);
            await using (slot)
            {
                most = System.Math.Max(most, Interlocked.Increment(ref running));
                await Task.Delay(20, TestContext.Current.CancellationToken);
                Interlocked.Decrement(ref running);
            }
        }));

        most.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AProbeSideReadWaitsUntilEveryBuildSideReadHasStartedWhenTheStrategyOrdersReads()
    {
        var gate = new FlowReadGate(new FlowReadGateSettings(2, true), 1);

        var probe = gate.Enter(false, TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        probe.IsCompleted.ShouldBeFalse();

        var build = await gate.Enter(true, TestContext.Current.CancellationToken);
        var probeSlot = await probe.WaitAsync(System.TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await probeSlot.DisposeAsync();
        await build.DisposeAsync();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ADegreeBelowOneIsRefused()
    {
        Should.Throw<System.ArgumentOutOfRangeException>(() => new FlowReadGate(new FlowReadGateSettings(0, false), 0));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    public async Task AJoinWhoseReadsAreGatedGivesTheSameRowsAsOneWhoseReadsAreNot(int degree, bool buildSideFirst)
    {
        var free = await Joined(null);
        var gated = await Joined(new FlowReadGate(new FlowReadGateSettings(degree, buildSideFirst), 1));

        gated.Select(Text).ShouldBe(free.Select(Text));
    }

    private static async Task<List<IDictionary<string, object?>>> Joined(FlowReadGate? gate)
    {
        IDictionary<string, object?>[] orders = [.. Enumerable.Range(1, 9).Select(id => Fields(("OrderId", id), ("PersonId", id % 3)))];
        IDictionary<string, object?>[] people = [.. Enumerable.Range(0, 3).Select(id => Fields(("PersonId", id), ("Name", $"p{id}")))];
        IFlowComponent left = FromRows("left", orders);
        IFlowComponent right = FromRows("right", people);
        if (gate is not null)
        {
            left = new FlowGatedRead(left, gate, false);
            right = new FlowGatedRead(right, gate, true);
        }

        var (sink, rows) = Sink("dst");
        var graph = Graph(
            [left, right, new FlowMergeJoin("join", ["PersonId"], ["PersonId"], JoinKinds.ByName("Inner"), "p_", batchSize: 2), sink],
            [Link("left", "join", input: "left"), Link("right", "join", input: "right"), Link("join", "dst")]);
        var result = await new FlowExecutor().Run(graph, Options(1), TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        return rows;
    }

    private static string Text(IDictionary<string, object?> row) => string.Join("|", row.Select(field => $"{field.Key}={field.Value}"));
}
