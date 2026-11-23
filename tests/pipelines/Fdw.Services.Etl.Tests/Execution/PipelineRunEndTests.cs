using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// FDW-7033: a run's end is always recorded and signaled. The metrics row is the run's second record; when it cannot be written (the host names no operational store, or the write fails) the failure
/// is logged with its code, and the execution item is still completed, the completion broadcast and the waiter signaled. Before, the end returned after the metrics row answered false, so whoever
/// awaited the run waited for ever.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineRunEndTests : IDisposable
{
    private static readonly Guid Run = Guid.Parse("11111111-0000-0000-0000-000000000001");

    public PipelineRunEndTests()
    {
        PipelineTypes.OperationalConnection = null;
    }

    public void Dispose()
    {
        PipelineTypes.OperationalConnection = null;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AMetricsRowThatCannotBeWrittenDoesNotStopTheEndBeingRecordedBroadcastAndSignaled()
    {
        var running = new Mock<IExecutionStateType>();
        running.SetupGet(s => s.IsTerminal).Returns(false);
        running.SetupGet(s => s.Name).Returns("Running");
        var item = new Mock<IExecutionItem>();
        item.SetupGet(i => i.State).Returns(running.Object);
        var tracker = new Mock<IExecutionTracker>();
        tracker.Setup(t => t.GetItem(Run, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IExecutionItem>.Success(item.Object));
        tracker.Setup(t => t.Complete(Run, false, "Exception", "it failed", It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var broadcaster = new Mock<IPipelineStatusBroadcaster>();
        broadcaster.Setup(b => b.BroadcastCompletion(It.IsAny<PipelineExecutionComplete>(), It.IsAny<Guid?>())).Returns(Task.CompletedTask);
        var signaler = new Mock<IExecutionCompletionSignaler>();

        await PipelineRequestProcessor.CompleteWithMetrics(
            new Mock<IDataGatewayProvider>(MockBehavior.Strict).Object, tracker.Object, broadcaster.Object, signaler.Object, NullLogger.Instance,
            Run, "Chat", null, false, "Exception", "it failed", 0, 0, 0, 0);

        tracker.Verify(t => t.Complete(Run, false, "Exception", "it failed", It.IsAny<CancellationToken>()), Times.Once);
        broadcaster.Verify(b => b.BroadcastCompletion(It.Is<PipelineExecutionComplete>(c => c.ExecutionId == Run && !c.Success), It.IsAny<Guid?>()), Times.Once);
        signaler.Verify(s => s.Signal(Run, false, "it failed"), Times.Once);
    }
}
