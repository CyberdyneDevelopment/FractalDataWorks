using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// A script source streams (FDW-4903): each batch leaves as soon as it is read, a failure part way through fails the run with its
/// cause, and whatever ends the read releases the script's reader.
/// </summary>
public sealed class ScriptSourceStreamingTests
{
    private static List<IDictionary<string, object?>> Rows(int count) =>
        [.. Enumerable.Range(0, count).Select(i => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Id"] = i })];

    // Ports whose one output keeps what it is sent and tells the test when it was sent something.
    private static (IFlowPorts Ports, List<FlowBatch> Batches) Ports(Action? onWrite = null)
    {
        var batches = new List<FlowBatch>();
        var output = new Mock<IFlowOutput>();
        output.SetupGet(o => o.Name).Returns(FlowScriptSource.OutputName);
        output
            .Setup(o => o.Write(It.IsAny<FlowBatch>(), It.IsAny<CancellationToken>()))
            .Callback((FlowBatch batch, CancellationToken _) =>
            {
                batches.Add(batch);
                onWrite?.Invoke();
            })
            .Returns(Task.CompletedTask);
        var ports = new Mock<IFlowPorts>();
        ports.Setup(p => p.Output(It.IsAny<string>())).Returns(output.Object);
        return (ports.Object, batches);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ABatchLeavesBeforeTheScriptHasBeenReadToItsEnd()
    {
        // The rows after the first batch wait until a batch has left: a source that read the whole script first would never send one.
        var firstBatchLeft = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var script = new StreamedScript(Rows(5), holdAfter: 2, gate: firstBatchLeft.Task);
        var (ports, batches) = Ports(() => firstBatchLeft.TrySetResult());
        var source = new FlowScriptSource("src", new ScriptingGateway([], stream: script).Gateway, "Warehouse", "SELECT Id FROM dbo.T", [], 10, 2);

        var result = await source.Run(ports, TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        batches.Select(batch => batch.Count).ShouldBe([2, 2, 1]);
        script.Released.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailurePartWayFailsTheRunWithItsCauseAndReleasesTheReader()
    {
        var cause = ScriptedRowsLog.ReadFailedPartWay(NullLogger.Instance, 2);
        var script = new StreamedScript(Rows(5), failAfter: 2, failure: cause);
        var (sink, _) = Sink("dst");
        var graph = Graph([new FlowScriptSource("src", new ScriptingGateway([], stream: script).Gateway, "Warehouse", "SELECT Id FROM dbo.T", [], 10, 2), sink], [Link("src", "dst")]);

        var result = await new FlowExecutor().Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("src");
        result.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(cause.Code);
        script.Released.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailurePartWayHasAlreadySentTheBatchesBeforeItAndTheSourceStillFails()
    {
        var script = new StreamedScript(Rows(5), failAfter: 3, failure: ScriptedRowsLog.ReadFailedPartWay(NullLogger.Instance, 3));
        var (ports, batches) = Ports();
        var source = new FlowScriptSource("src", new ScriptingGateway([], stream: script).Gateway, "Warehouse", "SELECT Id FROM dbo.T", [], 10, 2);

        var result = await source.Run(ports, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        batches.Select(batch => batch.Count).ShouldBe([2]);
        script.Released.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACancelledReadReleasesTheScriptsReader()
    {
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var script = new StreamedScript(Rows(10));
        var (ports, _) = Ports(cancel.Cancel);
        var source = new FlowScriptSource("src", new ScriptingGateway([], stream: script).Gateway, "Warehouse", "SELECT Id FROM dbo.T", [], 20, 2);

        await Should.ThrowAsync<OperationCanceledException>(() => source.Run(ports, cancel.Token));

        script.Released.ShouldBeTrue();
        script.HandedOut.ShouldBeLessThan(10);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ATestRunStopsReadingAtItsCapAndReleasesTheReader()
    {
        var script = new StreamedScript(Rows(10));
        var (ports, batches) = Ports();
        var source = new FlowScriptSource("src", new ScriptingGateway([], stream: script).Gateway, "Warehouse", "SELECT Id FROM dbo.T", [], 20, 2, rowCap: 3);

        var result = await source.Run(ports, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        batches.Select(batch => batch.Count).ShouldBe([2, 1]);
        script.HandedOut.ShouldBe(3);
        script.Released.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ATestRunCapAboveTheMostStillRefusesAScriptThatSelectsTooMany()
    {
        var script = new StreamedScript(Rows(10));
        var (ports, _) = Ports();
        var source = new FlowScriptSource("src", new ScriptingGateway([], stream: script).Gateway, "Warehouse", "SELECT Id FROM dbo.T", [], 3, 2, rowCap: 8);

        var result = await source.Run(ports, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Message.Code
            .ShouldBe(FlowScriptLog.TooManyRows(NullLogger.Instance, "src", "Warehouse", 3).Code);
        script.Released.ShouldBeTrue();
    }
}
