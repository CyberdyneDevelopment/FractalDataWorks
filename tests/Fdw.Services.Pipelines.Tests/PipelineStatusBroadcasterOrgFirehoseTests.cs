using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// Tests that <see cref="PipelineStatusBroadcaster"/> fans a lifecycle broadcast out to the owning
/// org's firehose group (<c>org:{orgId}:pipeline-updates</c>) when an org is supplied, and to no
/// firehose (only the pipeline/execution groups) when it is not — there is no global cross-org group.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class PipelineStatusBroadcasterOrgFirehoseTests
{
    private static (PipelineStatusBroadcaster Broadcaster, List<string> Groups) CreateBroadcaster()
    {
        var targeted = new List<string>();
        var client = new Mock<IPipelineStatusHubClient>();
        var clients = new Mock<IHubClients<IPipelineStatusHubClient>>();
        clients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string g) => { targeted.Add(g); return client.Object; });
        var hubContext = new Mock<IHubContext<PipelineStatusHub, IPipelineStatusHubClient>>();
        hubContext.SetupGet(h => h.Clients).Returns(clients.Object);
        return (new PipelineStatusBroadcaster(hubContext.Object), targeted);
    }

    [Fact]
    public async Task StatusChangeWithOrgTargetsOrgFirehose()
    {
        var (broadcaster, groups) = CreateBroadcaster();
        var orgId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var executionId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        await broadcaster.BroadcastStatusChange("nfl", executionId, "Running", orgId: orgId);

        groups.ShouldContain($"org:{orgId}:pipeline-updates");
        groups.ShouldContain("pipeline:nfl");
        groups.ShouldContain($"execution:{executionId}");
    }

    [Fact]
    public async Task CompletionWithOrgTargetsOrgFirehose()
    {
        var (broadcaster, groups) = CreateBroadcaster();
        var orgId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var executionId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        await broadcaster.BroadcastCompletion(
            new PipelineExecutionComplete { PipelineName = "nfl", ExecutionId = executionId, Status = "Succeeded" },
            orgId);

        groups.ShouldContain($"org:{orgId}:pipeline-updates");
    }

    [Fact]
    public async Task StatusChangeWithoutOrgTargetsNoFirehose()
    {
        var (broadcaster, groups) = CreateBroadcaster();
        var executionId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        await broadcaster.BroadcastStatusChange("nfl", executionId, "Running");

        groups.ShouldNotContain(g => g.StartsWith("org:", StringComparison.Ordinal));
        groups.ShouldContain("pipeline:nfl");
        groups.ShouldContain($"execution:{executionId}");
    }
}
