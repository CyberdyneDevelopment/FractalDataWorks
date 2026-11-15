using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Fdw.Services.Etl.Projects.Execution;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Resiliency.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Tests.Execution;

/// <summary>
/// The statuses an orchestration run broadcasts about itself reach the organization that owns the pipelines it runs (FDW-5986).
/// </summary>
public sealed class OrchestrationNodeOrchestratorBroadcastTests
{
    private static readonly Guid Org = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a");
    private static readonly Guid Pipeline = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Node = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Run = Guid.Parse("99999999-0000-0000-0000-000000000009");
    private static readonly Guid Tenant = Guid.Parse("44444444-0000-0000-0000-000000000004");
    private static readonly Guid Started = Guid.Parse("55555555-0000-0000-0000-000000000005");

    private static async Task<Mock<IPipelineStatusBroadcaster>> Execute(bool pipelineSucceeds, Guid? pipelineOrg)
    {
        var root = new OrchestrationNodeImplementationConfiguration
        {
            Id = Node,
            NodeTypeId = OrchestrationNodeTypes.ByName("Step").Id,
            Name = "Nightly",
            Children = [],
            PipelineMemberships = [new OrchestrationNodePipelineMembershipConfiguration { PipelineId = Pipeline, Ordinal = 0 }]
        };
        var nodes = new Mock<IOrchestrationNodeConfigurationProvider>();
        nodes
            .Setup(n => n.Get(Node, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IOrchestrationNodeImplementationConfiguration>.Success(root));
        var policy = new ExecutionPolicySnapshot("ContinueStage", "ContinueProject", 1, false, false, false, null);
        var resolver = new Mock<IEffectivePolicyResolver>();
        resolver
            .Setup(r => r.ResolveForRootNode(It.IsAny<IOrchestrationNodeImplementationConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ExecutionPolicySnapshot>.Success(policy));
        var tracker = new Mock<IExecutionTracker>();
        tracker
            .Setup(t => t.TransitionState(It.IsAny<Guid>(), It.IsAny<IExecutionStateType>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        tracker
            .Setup(t => t.Complete(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var starter = new Mock<IPipelineRunStarter>();
        starter
            .Setup(s => s.Start(It.IsAny<PipelineRunStart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Success(Started));
        starter.Setup(s => s.Await(Started, It.IsAny<CancellationToken>())).ReturnsAsync(pipelineSucceeds);
        var configuration = new Mock<IPipelineImplementationConfiguration>();
        configuration.SetupGet(c => c.OrgId).Returns(pipelineOrg);
        var pipelines = new Mock<IPipelineConfigurationProvider>();
        pipelines
            .Setup(p => p.Get(Pipeline, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(configuration.Object));
        var broadcaster = new Mock<IPipelineStatusBroadcaster>();

        var orchestrator = new OrchestrationNodeOrchestrator(
            nodes.Object, resolver.Object, Mock.Of<IResiliencyExecutor>(), tracker.Object, starter.Object, broadcaster.Object, pipelines.Object,
            NullLogger<OrchestrationNodeOrchestrator>.Instance);

        await orchestrator.Execute(
            new OrchestrationNodeExecutionRequest { ExecutionId = Run, RootNodeId = Node, TriggerSource = "Api", TenantId = Tenant, RunRoles = ["etl-runner"] },
            TestContext.Current.CancellationToken);
        return broadcaster;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunsStartAndSuccessAreBroadcastToTheOrganizationOfItsPipelines()
    {
        var broadcaster = await Execute(pipelineSucceeds: true, pipelineOrg: Org);

        broadcaster.Verify(b => b.BroadcastStatusChange("Nightly", Run, "Running", null, Org), Times.Once);
        broadcaster.Verify(b => b.BroadcastStatusChange("Nightly", Run, "Succeeded", null, Org), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunsFailureIsBroadcastToTheOrganizationOfItsPipelines()
    {
        var broadcaster = await Execute(pipelineSucceeds: false, pipelineOrg: Org);

        broadcaster.Verify(b => b.BroadcastStatusChange("Nightly", Run, "Failed", null, Org), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ARunWhosePipelinesStateNoOrganizationIsStillBroadcastToItsOwnGroupsWithNone()
    {
        var broadcaster = await Execute(pipelineSucceeds: true, pipelineOrg: null);

        broadcaster.Verify(b => b.BroadcastStatusChange("Nightly", Run, "Running", null, null), Times.Once);
        broadcaster.Verify(b => b.BroadcastStatusChange("Nightly", Run, "Succeeded", null, null), Times.Once);
    }
}
