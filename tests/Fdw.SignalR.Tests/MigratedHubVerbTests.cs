using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.SignalR;
using Fdw.Services.Data.SignalR;
using Fdw.Services.Messaging.Hubs;
using Fdw.Services.Pipelines.Hubs;
using Fdw.SignalR.Tests.Doubles;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace Fdw.SignalR.Tests;

/// <summary>
/// Tests that each migrated domain hub builds the correct group key through the inherited
/// <see cref="Fdw.SignalR.RealTimeHubBase{TClient}"/> subscribe contract and joins the correct group on connect.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class MigratedHubVerbTests
{
    [Fact]
    public async Task PipelineHubSubscribeVerbsBuildExpectedGroups()
    {
        var groups = HubFixtures.GroupManager();
        var hub = new PipelineStatusHub(new RecordingLogger<PipelineStatusHub>())
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = groups.Object,
        };
        var executionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await hub.SubscribeToPipeline("nfl");
        await hub.SubscribeToExecution(executionId);
        await hub.UnsubscribeFromPipeline("nfl");
        await hub.UnsubscribeFromExecution(executionId);

        groups.Verify(g => g.AddToGroupAsync("conn-1", "pipeline:nfl", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.AddToGroupAsync("conn-1", $"execution:{executionId}", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", "pipeline:nfl", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", $"execution:{executionId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PipelineHubOnConnectJoinsOrgFirehoseFromClaim()
    {
        // Why: the per-org firehose replaces the removed global "pipeline-updates" group — a connection
        // joins only its own org's group org:{orgId}:pipeline-updates, read from the JWT org_id claim
        // (FDW-545). No cross-org global firehose exists.
        var groups = HubFixtures.GroupManager();
        const string orgId = "22222222-2222-2222-2222-222222222222";
        var hub = new PipelineStatusHub(new RecordingLogger<PipelineStatusHub>())
        {
            Context = HubFixtures.Context("conn-1", HubFixtures.UserWithOrg(orgId)).Object,
            Groups = groups.Object,
        };

        await hub.OnConnectedAsync();

        groups.Verify(
            g => g.AddToGroupAsync("conn-1", $"org:{orgId}:pipeline-updates", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PipelineHubOnConnectWithNoOrgClaimJoinsNoGroup()
    {
        // Why: a connection with no org_id claim joins no firehose — there is no global cross-org
        // group and no placeholder org is substituted (NO FALLBACKS).
        var groups = HubFixtures.GroupManager();
        var hub = new PipelineStatusHub(new RecordingLogger<PipelineStatusHub>())
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = groups.Object,
        };

        await hub.OnConnectedAsync();

        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CalculationHubVerbsAndConnectBuildExpectedGroups()
    {
        var groups = HubFixtures.GroupManager();
        var hub = new CalculationHub(new RecordingLogger<CalculationHub>())
        {
            Context = HubFixtures.Context("conn-1", HubFixtures.UserWithName("bob")).Object,
            Groups = groups.Object,
        };

        await hub.OnConnectedAsync();
        await hub.SubscribeToCalculation("c7");
        await hub.SubscribeToAllCalculations();
        await hub.UnsubscribeFromCalculation("c7");

        groups.Verify(g => g.AddToGroupAsync("conn-1", "user:bob", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.AddToGroupAsync("conn-1", "calc:c7", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.AddToGroupAsync("conn-1", "all-calculations", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", "calc:c7", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SchemaDiscoveryHubVerbsAndConnectBuildExpectedGroups()
    {
        var groups = HubFixtures.GroupManager();
        var hub = new SchemaDiscoveryHub(new RecordingLogger<SchemaDiscoveryHub>())
        {
            Context = HubFixtures.Context("conn-1", HubFixtures.UserWithName("carol")).Object,
            Groups = groups.Object,
        };

        await hub.OnConnectedAsync();
        await hub.SubscribeToDiscovery("d9");
        await hub.SubscribeToAllDiscoveries();
        await hub.UnsubscribeFromDiscovery("d9");

        groups.Verify(g => g.AddToGroupAsync("conn-1", "user:carol", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.AddToGroupAsync("conn-1", "discovery:d9", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.AddToGroupAsync("conn-1", "all-discoveries", It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", "discovery:d9", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MessageHubJoinAndLeaveUseRawUserGroup()
    {
        var groups = HubFixtures.GroupManager();
        var hub = new MessageHub(new RecordingLogger<MessageHub>())
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = groups.Object,
        };
        const string userId = "8a1f0c1e-0000-0000-0000-000000000001";

        await hub.OnConnectedAsync();
        await hub.JoinUserGroup(userId);
        await hub.LeaveUserGroup(userId);
        await hub.OnDisconnectedAsync(exception: null);

        groups.Verify(g => g.AddToGroupAsync("conn-1", userId, It.IsAny<CancellationToken>()), Times.Once);
        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
