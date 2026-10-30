using System;
using System.Threading.Tasks;
using Fdw.Services.Etl.Projects.Execution;

namespace Fdw.Services.Etl.Projects.Tests.Execution;

/// <summary>
/// Tests that the project and orchestration-node execution queues refuse a request past their capacity (FDW-3181): a full channel answers
/// false to TryWrite only in the Wait full mode, and in the DropWrite mode it discarded the request and answered true.
/// </summary>
public sealed class ExecutionQueueFullTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheProjectQueueRefusesTheRequestPastItsCapacity()
    {
        var queue = new ProjectExecutionQueue(2);

        (await queue.Enqueue(new ProjectExecutionRequest { ExecutionId = Guid.NewGuid(), ProjectName = "P", TriggerSource = "Test" }, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await queue.Enqueue(new ProjectExecutionRequest { ExecutionId = Guid.NewGuid(), ProjectName = "P", TriggerSource = "Test" }, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await queue.Enqueue(new ProjectExecutionRequest { ExecutionId = Guid.NewGuid(), ProjectName = "P", TriggerSource = "Test" }, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheOrchestrationNodeQueueRefusesTheRequestPastItsCapacity()
    {
        var queue = new OrchestrationNodeExecutionQueue(2);

        (await queue.Enqueue(Request(), TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await queue.Enqueue(Request(), TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await queue.Enqueue(Request(), TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    private static OrchestrationNodeExecutionRequest Request() => new()
    {
        ExecutionId = Guid.NewGuid(),
        RootNodeId = Guid.NewGuid(),
        TriggerSource = "Test",
        RunRoles = ["DataWriter"]
    };
}
