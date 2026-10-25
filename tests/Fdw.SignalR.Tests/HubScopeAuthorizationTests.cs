using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.SignalR.Tests.Doubles;
using Microsoft.AspNetCore.SignalR;
using Xunit;

namespace Fdw.SignalR.Tests;

/// <summary>
/// FDW-2792: a connection may join the scope of the person it is signed in as and of the organization it belongs to, and no other person's or
/// organization's. The rule is the base class's default, so every hub has it.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Security")]
public sealed class HubScopeAuthorizationTests
{
    private const int SubscriptionRejectedNotAuthorized = 71004;
    private const string Me = "8a1f0c1e-0000-0000-0000-000000000001";
    private const string Someone = "8a1f0c1e-0000-0000-0000-000000000002";

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "test"));

    private static (TestHub Hub, Mock<IGroupManager> Groups, RecordingLogger<TestHub> Logger) Hub(ClaimsPrincipal? user)
    {
        var groups = HubFixtures.GroupManager();
        var logger = new RecordingLogger<TestHub>();
        var hub = new TestHub(logger)
        {
            Context = HubFixtures.Context("conn-1", user).Object,
            Groups = groups.Object,
        };
        return (hub, groups, logger);
    }

    private static void Joined(Mock<IGroupManager> groups, string key)
        => groups.Verify(g => g.AddToGroupAsync("conn-1", key, It.IsAny<CancellationToken>()), Times.Once);

    private static void NothingJoined(Mock<IGroupManager> groups)
        => groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public void TheScopeNamesAreTheOnesTheHubsAndPublishersUse()
    {
        HubScopes.User("abc").ShouldBe("user:abc");
        HubScopes.Org("o1", "pipeline-updates").ShouldBe("org:o1:pipeline-updates");
    }

    [Fact]
    public async Task APersonJoinsTheirOwnUserScope()
    {
        var (hub, groups, _) = Hub(Principal(new Claim("sub", Me)));

        await hub.Subscribe(HubScopes.User(Me));

        Joined(groups, HubScopes.User(Me));
    }

    [Fact]
    public async Task APersonIsRefusedAnotherPersonsUserScope()
    {
        var (hub, groups, logger) = Hub(Principal(new Claim("sub", Me)));

        await hub.Subscribe(HubScopes.User(Someone));

        NothingJoined(groups);
        logger.Entries.ShouldContain(e => e.EventId == SubscriptionRejectedNotAuthorized);
    }

    [Fact]
    public async Task TheNameIdentifierClaimNamesThePersonWhenThereIsNoSubClaim()
    {
        var (hub, groups, _) = Hub(Principal(new Claim(ClaimTypes.NameIdentifier, Me)));

        await hub.Subscribe(HubScopes.User(Me));

        Joined(groups, HubScopes.User(Me));
    }

    [Fact]
    public async Task ADisplayNameIsNotAnIdAndJoinsNoUserScope()
    {
        var (hub, groups, _) = Hub(Principal(new Claim(ClaimTypes.Name, Me)));

        await hub.Subscribe(HubScopes.User(Me));

        NothingJoined(groups);
    }

    [Fact]
    public async Task AConnectionWithNoIdentityJoinsNoUserScope()
    {
        var (hub, groups, _) = Hub(user: null);

        await hub.Subscribe(HubScopes.User(Me));

        NothingJoined(groups);
    }

    [Fact]
    public async Task TheCaseOfTheIdDoesNotDecideWhoThePersonIs()
    {
        var (hub, groups, _) = Hub(Principal(new Claim("sub", Me.ToUpperInvariant())));

        await hub.Subscribe(HubScopes.User(Me));

        Joined(groups, HubScopes.User(Me));
    }

    [Fact]
    public async Task AUserScopeThatOnlyStartsLikeTheCallersIdIsRefused()
    {
        var (hub, groups, _) = Hub(Principal(new Claim("sub", Me)));

        await hub.Subscribe(HubScopes.User(Me + "0"));

        NothingJoined(groups);
    }

    [Fact]
    public async Task AMemberJoinsTheirOwnOrganizationsScope()
    {
        var (hub, groups, _) = Hub(Principal(new Claim("org_id", "org-1")));

        await hub.Subscribe(HubScopes.Org("org-1", "pipeline-updates"));

        Joined(groups, HubScopes.Org("org-1", "pipeline-updates"));
    }

    [Fact]
    public async Task AMemberIsRefusedAnotherOrganizationsScope()
    {
        var (hub, groups, logger) = Hub(Principal(new Claim("org_id", "org-1")));

        await hub.Subscribe(HubScopes.Org("org-2", "pipeline-updates"));

        NothingJoined(groups);
        logger.Entries.ShouldContain(e => e.EventId == SubscriptionRejectedNotAuthorized);
    }

    [Fact]
    public async Task AnOrganizationScopeWhoseIdOnlyStartsLikeTheCallersIsRefused()
    {
        var (hub, groups, _) = Hub(Principal(new Claim("org_id", "org-1")));

        await hub.Subscribe(HubScopes.Org("org-10", "pipeline-updates"));

        NothingJoined(groups);
    }

    [Fact]
    public async Task AConnectionWithNoOrganizationJoinsNoOrganizationScope()
    {
        var (hub, groups, _) = Hub(Principal(new Claim("sub", Me)));

        await hub.Subscribe(HubScopes.Org("org-1", "pipeline-updates"));

        NothingJoined(groups);
    }

    [Fact]
    public async Task AResourceScopeIsStillTheHubsToDecideAndIsAllowedByDefault()
    {
        var (hub, groups, _) = Hub(Principal(new Claim("sub", Me)));

        await hub.Subscribe("execution:abc");

        Joined(groups, "execution:abc");
    }

    [Fact]
    public async Task SubscribeAwaitsTheAsyncJoinCheckAndJoinsWhenItAllows()
    {
        var (hub, groups, _) = Hub(Principal(new Claim("sub", Me)));
        hub.AuthorizeJoinOverride = async key =>
        {
            await Task.Yield();
            return key == "execution:abc";
        };

        await hub.Subscribe("execution:abc");

        Joined(groups, "execution:abc");
    }

    [Fact]
    public async Task SubscribeAwaitsTheAsyncJoinCheckAndRefusesWhenItDoesNot()
    {
        var (hub, groups, logger) = Hub(Principal(new Claim("sub", Me)));
        hub.AuthorizeJoinOverride = async _ =>
        {
            await Task.Yield();
            return false;
        };

        await hub.Subscribe("execution:abc");

        NothingJoined(groups);
        logger.Entries.ShouldContain(e => e.EventId == SubscriptionRejectedNotAuthorized);
    }

    [Fact]
    public async Task AHubThatOverridesOnlyTheSynchronousCheckStillWorksThroughTheAsyncSeam()
    {
        var (hub, groups, _) = Hub(Principal(new Claim("sub", Me)));
        hub.CanJoinOverride = key => key == "ok:1";

        await hub.Subscribe("ok:1");
        await hub.Subscribe("no:1");

        Joined(groups, "ok:1");
        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), "no:1", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void TheCallersTenantIsTheTenantIdClaimAndNothingElse()
    {
        var (hub, _, _) = Hub(Principal(new Claim("tenantId", "t-1"), new Claim("org_id", "o-1")));
        hub.CallerTenantIdPublic.ShouldBe("t-1");
        Hub(Principal(new Claim("org_id", "o-1"))).Hub.CallerTenantIdPublic.ShouldBeNull();
    }
}
