using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Messaging.Hubs;
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
    private static (MessageHub Hub, Mock<IGroupManager> Groups) CreateHub(string connectionId = "conn-1")
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
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public async Task JoinUserGroupAddsConnectionToGroupNamedByUserId()
    {
        var (hub, groups) = CreateHub("conn-1");
        var userId = "user-42";

        await hub.JoinUserGroup(userId);

        groups.Verify(g => g.AddToGroupAsync("conn-1", userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public async Task LeaveUserGroupRemovesConnectionFromGroupNamedByUserId()
    {
        var (hub, groups) = CreateHub("conn-1");
        var userId = "user-42";

        await hub.LeaveUserGroup(userId);

        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
