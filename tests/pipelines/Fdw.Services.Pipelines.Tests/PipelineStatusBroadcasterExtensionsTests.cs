using System;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Notifications;
using Moq;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// Tests that a run is announced from its pipeline's configuration (FDW-5943): the name and the organization both come from it.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public sealed class PipelineStatusBroadcasterExtensionsTests
{
    private static readonly Guid Organization = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Run = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static Mock<IPipelineImplementationConfiguration> Pipeline(Guid? orgId)
    {
        var pipeline = new Mock<IPipelineImplementationConfiguration>();
        pipeline.SetupGet(p => p.Name).Returns("nfl");
        pipeline.SetupGet(p => p.OrgId).Returns(orgId);
        return pipeline;
    }

    [Fact]
    public async Task ARunIsAnnouncedWithThePipelinesNameAndTheOrganizationThatOwnsIt()
    {
        var broadcaster = new Mock<IPipelineStatusBroadcaster>();

        await broadcaster.Object.BroadcastRunStatus(Pipeline(Organization).Object, Run, "Triggered");

        broadcaster.Verify(b => b.BroadcastStatusChange("nfl", Run, "Triggered", null, Organization), Times.Once);
        broadcaster.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AMessageIsCarriedWithTheStatus()
    {
        var broadcaster = new Mock<IPipelineStatusBroadcaster>();

        await broadcaster.Object.BroadcastRunStatus(Pipeline(Organization).Object, Run, "AwaitingApproval", "Held for approval");

        broadcaster.Verify(b => b.BroadcastStatusChange("nfl", Run, "AwaitingApproval", "Held for approval", Organization), Times.Once);
    }

    [Fact]
    public async Task APipelineThatStatesNoOrganizationIsAnnouncedToNone()
    {
        var broadcaster = new Mock<IPipelineStatusBroadcaster>();

        await broadcaster.Object.BroadcastRunStatus(Pipeline(null).Object, Run, "Triggered");

        broadcaster.Verify(b => b.BroadcastStatusChange("nfl", Run, "Triggered", null, null), Times.Once);
    }
}
