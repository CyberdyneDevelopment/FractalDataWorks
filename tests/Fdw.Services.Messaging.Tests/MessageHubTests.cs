using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Messaging.Hubs;
using Fdw.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// Tests for <see cref="MessageHub"/> - the thin per-user group join/leave wrapper over
/// <c>RealTimeHubBase{TClient}.Subscribe/Unsubscribe</c> (whose own guard/logging branches are
/// covered generically in Fdw.SignalR.Tests).
/// </summary>
public sealed class MessageHubTests
{
    private const string Caller = "8a1f0c1e-0000-0000-0000-000000000001";
    private const string Other = "8a1f0c1e-0000-0000-0000-000000000002";

    private static ClaimsPrincipal SignedInAs(string userId)
        => new(new ClaimsIdentity([new Claim("sub", userId)], "test"));

    private static (MessageHub Hub, Mock<IGroupManager> Groups) CreateHub(string connectionId = "conn-1", ClaimsPrincipal? user = null)
    {
        var groups = new Mock<IGroupManager>();
        groups
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groups
            .Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.ConnectionId).Returns(connectionId);
        context.SetupGet(c => c.User).Returns(user);

        var hub = new MessageHub(NullLogger<MessageHub>.Instance)
        {
            Context = context.Object,
            Groups = groups.Object,
        };
        return (hub, groups);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void HubNameIsMessage()
    {
        var property = typeof(MessageHub).GetProperty("HubName", BindingFlags.NonPublic | BindingFlags.Instance);
        property.ShouldNotBeNull();

        var (hub, _) = CreateHub();
        var value = property!.GetValue(hub) as string;

        value.ShouldBe("Message");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task JoinUserGroupAddsTheCallerToItsOwnUserScope()
    {
        var (hub, groups) = CreateHub("conn-1", SignedInAs(Caller));

        await hub.JoinUserGroup(Caller);

        groups.Verify(g => g.AddToGroupAsync("conn-1", HubScopes.User(Caller), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task JoinUserGroupRefusesAnotherPersonsIdAndJoinsNothing()
    {
        var (hub, groups) = CreateHub("conn-1", SignedInAs(Caller));

        await hub.JoinUserGroup(Other);

        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task JoinUserGroupRefusesAnIdThatIsNotAGuidUnlessItIsTheCallers()
    {
        var (hub, groups) = CreateHub("conn-1", SignedInAs(Caller));

        await hub.JoinUserGroup("user-42");

        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task JoinUserGroupRefusesAConnectionThatIsNotSignedInAsAnyone()
    {
        var (hub, groups) = CreateHub("conn-1");

        await hub.JoinUserGroup(Caller);

        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task JoinUserGroupWritesAGuidInTheCanonicalFormMessagesArePublishedTo()
    {
        var (hub, groups) = CreateHub("conn-1", SignedInAs(Caller.ToUpperInvariant()));

        await hub.JoinUserGroup(Caller.ToUpperInvariant());

        groups.Verify(g => g.AddToGroupAsync("conn-1", HubScopes.User(Caller), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public async Task LeaveUserGroupRemovesConnectionFromItsUserScope()
    {
        var (hub, groups) = CreateHub("conn-1", SignedInAs(Caller));

        await hub.LeaveUserGroup(Caller);

        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", HubScopes.User(Caller), It.IsAny<CancellationToken>()), Times.Once);
    }
}
