using System.Threading;
using System.Threading.Tasks;
using Fdw.SignalR.Tests.Doubles;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace Fdw.SignalR.Tests;

/// <summary>
/// Tests for <see cref="RealTimeHubBase{TClient}"/> — the realtime building-block base hub.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class RealTimeHubBaseTests
{
    private const int ClientConnected = 11004;
    private const int ClientDisconnected = 11005;
    private const int ClientDisconnectedWithError = 71002;
    private const int ClientJoinedGroup = 11006;
    private const int ClientLeftGroup = 11007;
    private const int SubscriptionRejectedEmptyScope = 71003;
    private const int SubscriptionRejectedNotAuthorized = 71004;
    private const int HubIdentityMissing = 71005;

    [Fact]
    public async Task SubscribeWithValidScopeJoinsGroupAndLogs()
    {
        var groups = HubFixtures.GroupManager();
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = groups.Object,
        };

        await hub.Subscribe("execution:abc");

        groups.Verify(g => g.AddToGroupAsync("conn-1", "execution:abc", It.IsAny<CancellationToken>()), Times.Once);
        logger.Entries.ShouldContain(e => e.EventId == ClientJoinedGroup);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SubscribeWithEmptyScopeRejectsAndDoesNotJoin(string scope)
    {
        var groups = HubFixtures.GroupManager();
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = groups.Object,
        };

        await hub.Subscribe(scope);

        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        logger.Entries.ShouldContain(e => e.EventId == SubscriptionRejectedEmptyScope);
    }

    [Fact]
    public async Task SubscribeWhenNotAuthorizedRejectsAndDoesNotJoin()
    {
        var groups = HubFixtures.GroupManager();
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = groups.Object,
            CanJoinOverride = _ => false,
        };

        await hub.Subscribe("execution:abc");

        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        logger.Entries.ShouldContain(e => e.EventId == SubscriptionRejectedNotAuthorized);
    }

    [Fact]
    public async Task UnsubscribeWithValidScopeLeavesGroupAndLogs()
    {
        var groups = HubFixtures.GroupManager();
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = groups.Object,
        };

        await hub.Unsubscribe("execution:abc");

        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", "execution:abc", It.IsAny<CancellationToken>()), Times.Once);
        logger.Entries.ShouldContain(e => e.EventId == ClientLeftGroup);
    }

    [Fact]
    public async Task UnsubscribeWithEmptyScopeRejectsAndDoesNotLeave()
    {
        var groups = HubFixtures.GroupManager();
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = groups.Object,
        };

        await hub.Unsubscribe("  ");

        groups.Verify(
            g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        logger.Entries.ShouldContain(e => e.EventId == SubscriptionRejectedEmptyScope);
    }

    [Fact]
    public async Task OnConnectedLogsAndInvokesOnJoin()
    {
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = HubFixtures.GroupManager().Object,
        };

        await hub.OnConnectedAsync();

        hub.OnJoinInvoked.ShouldBeTrue();
        logger.Entries.ShouldContain(e => e.EventId == ClientConnected);
    }

    [Fact]
    public async Task OnDisconnectedWithoutErrorLogsCleanDisconnect()
    {
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = HubFixtures.GroupManager().Object,
        };

        await hub.OnDisconnectedAsync(exception: null);

        logger.Entries.ShouldContain(e => e.EventId == ClientDisconnected);
        logger.Entries.ShouldNotContain(e => e.EventId == ClientDisconnectedWithError);
    }

    [Fact]
    public async Task OnDisconnectedWithErrorLogsErrorDisconnect()
    {
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = HubFixtures.GroupManager().Object,
        };

        await hub.OnDisconnectedAsync(new System.InvalidOperationException("boom"));

        logger.Entries.ShouldContain(e => e.EventId == ClientDisconnectedWithError);
    }

    [Fact]
    public async Task JoinAuthenticatedUserScopeWithIdentityJoinsUserGroup()
    {
        var groups = HubFixtures.GroupManager();
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1", HubFixtures.UserWithName("alice")).Object,
            Groups = groups.Object,
        };

        await hub.JoinAuthenticatedUserScopePublic();

        groups.Verify(g => g.AddToGroupAsync("conn-1", "user:alice", It.IsAny<CancellationToken>()), Times.Once);
        hub.AuthenticatedUserIdPublic.ShouldBe("alice");
    }

    [Fact]
    public async Task JoinAuthenticatedUserScopeWithAnonymousIdentitySkipsAndLogs()
    {
        var groups = HubFixtures.GroupManager();
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1", HubFixtures.Anonymous()).Object,
            Groups = groups.Object,
        };

        await hub.JoinAuthenticatedUserScopePublic();

        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        logger.Entries.ShouldContain(e => e.EventId == HubIdentityMissing);
        hub.AuthenticatedUserIdPublic.ShouldBeNull();
    }

    [Fact]
    public async Task JoinAuthenticatedUserScopeWithNullUserSkipsAndLogs()
    {
        var groups = HubFixtures.GroupManager();
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            // Context with no User at all — exercises the null-principal path of AuthenticatedUserId.
            Context = HubFixtures.Context("conn-1", user: null).Object,
            Groups = groups.Object,
        };

        await hub.JoinAuthenticatedUserScopePublic();

        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        logger.Entries.ShouldContain(e => e.EventId == HubIdentityMissing);
        hub.AuthenticatedUserIdPublic.ShouldBeNull();
    }

    [Fact]
    public async Task JoinAuthenticatedUserScopeWithoutIdentitySkipsAndLogs()
    {
        var groups = HubFixtures.GroupManager();
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            // Principal present but Identity is null — exercises the inner null-conditional branch.
            Context = HubFixtures.Context("conn-1", HubFixtures.WithoutIdentity()).Object,
            Groups = groups.Object,
        };

        await hub.JoinAuthenticatedUserScopePublic();

        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        logger.Entries.ShouldContain(e => e.EventId == HubIdentityMissing);
        hub.AuthenticatedUserIdPublic.ShouldBeNull();
    }

    [Fact]
    public async Task SubscribeWithNullLoggerDoesNotThrow()
    {
        var groups = HubFixtures.GroupManager();
        var hub = new TestHub(logger: null)
        {
            Context = HubFixtures.Context("conn-1").Object,
            Groups = groups.Object,
        };

        await Should.NotThrowAsync(() => hub.Subscribe("execution:abc"));
        groups.Verify(g => g.AddToGroupAsync("conn-1", "execution:abc", It.IsAny<CancellationToken>()), Times.Once);
    }
}
