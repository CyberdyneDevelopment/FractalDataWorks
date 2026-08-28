using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// Covers <see cref="PipelineStatusHub"/>'s auto-join branch (<c>OnJoin</c>, invoked via
/// <c>OnConnectedAsync</c>): a connection whose principal carries an <c>org_id</c> claim joins the
/// org firehose group, while a connection with no principal / no claim joins no group at all (NO
/// FALLBACKS — never a placeholder org). Also covers the four thin subscribe/unsubscribe verbs.
/// <see cref="Hub{T}.Context"/> and <see cref="Hub{T}.Groups"/> are publicly settable, so the hub is
/// exercised directly without a live SignalR connection.
/// </summary>
[Trait("Category", "CoreFramework")]
public sealed class PipelineStatusHubTests
{
    private static (PipelineStatusHub Hub, Mock<IGroupManager> Groups, string ConnectionId) CreateHub(ClaimsPrincipal? user)
    {
        var connectionId = Guid.NewGuid().ToString();
        var contextMock = new Mock<HubCallerContext>();
        contextMock.SetupGet(c => c.ConnectionId).Returns(connectionId);
        contextMock.SetupGet(c => c.User).Returns(user);

        var groupsMock = new Mock<IGroupManager>();
        groupsMock
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groupsMock
            .Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new PipelineStatusHub(NullLogger<PipelineStatusHub>.Instance)
        {
            Context = contextMock.Object,
            Groups = groupsMock.Object,
        };
        return (hub, groupsMock, connectionId);
    }

    private static ClaimsPrincipal PrincipalWithOrgClaim(Guid orgId) =>
        new(new ClaimsIdentity([new Claim("org_id", orgId.ToString())]));

    // ------------------------------------------------------------------
    // OnJoin (via OnConnectedAsync)
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task OnConnectedAsyncWithOrgClaimJoinsOrgFirehoseGroup()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var (hub, groups, connectionId) = CreateHub(PrincipalWithOrgClaim(orgId));

        // Act
        await hub.OnConnectedAsync();

        // Assert
        groups.Verify(
            g => g.AddToGroupAsync(connectionId, $"org:{orgId}:pipeline-updates", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task OnConnectedAsyncWithoutOrgClaimJoinsNoGroup()
    {
        // Arrange
        var principalWithNoOrgClaim = new ClaimsPrincipal(new ClaimsIdentity());
        var (hub, groups, _) = CreateHub(principalWithNoOrgClaim);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task OnConnectedAsyncWithNullUserJoinsNoGroup()
    {
        // Arrange
        var (hub, groups, _) = CreateHub(user: null);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ------------------------------------------------------------------
    // Subscribe / Unsubscribe verbs
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SubscribeToPipelineJoinsPipelineGroup()
    {
        var (hub, groups, connectionId) = CreateHub(user: null);

        await hub.SubscribeToPipeline("nfl");

        groups.Verify(g => g.AddToGroupAsync(connectionId, "pipeline:nfl", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task UnsubscribeFromPipelineLeavesPipelineGroup()
    {
        var (hub, groups, connectionId) = CreateHub(user: null);

        await hub.UnsubscribeFromPipeline("nfl");

        groups.Verify(g => g.RemoveFromGroupAsync(connectionId, "pipeline:nfl", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SubscribeToExecutionJoinsExecutionGroup()
    {
        var executionId = Guid.NewGuid();
        var (hub, groups, connectionId) = CreateHub(user: null);

        await hub.SubscribeToExecution(executionId);

        groups.Verify(g => g.AddToGroupAsync(connectionId, $"execution:{executionId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task UnsubscribeFromExecutionLeavesExecutionGroup()
    {
        var executionId = Guid.NewGuid();
        var (hub, groups, connectionId) = CreateHub(user: null);

        await hub.UnsubscribeFromExecution(executionId);

        groups.Verify(g => g.RemoveFromGroupAsync(connectionId, $"execution:{executionId}", It.IsAny<CancellationToken>()), Times.Once);
    }
}
