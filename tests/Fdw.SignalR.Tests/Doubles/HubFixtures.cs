using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Fdw.SignalR.Tests.Doubles;

/// <summary>
/// Helpers for wiring a <see cref="Fdw.SignalR.RealTimeHubBase{TClient}"/> under test with a mock
/// <see cref="HubCallerContext"/> and <see cref="IGroupManager"/>.
/// </summary>
public static class HubFixtures
{
    /// <summary>Creates a mock caller context for <paramref name="connectionId"/> and optional user.</summary>
    public static Mock<HubCallerContext> Context(string connectionId, ClaimsPrincipal? user = null)
    {
        var ctx = new Mock<HubCallerContext>();
        ctx.SetupGet(c => c.ConnectionId).Returns(connectionId);
        ctx.SetupGet(c => c.User).Returns(user);
        return ctx;
    }

    /// <summary>Creates a group manager whose add/remove operations complete synchronously.</summary>
    public static Mock<IGroupManager> GroupManager()
    {
        var groups = new Mock<IGroupManager>();
        groups
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groups
            .Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return groups;
    }

    /// <summary>Builds an authenticated principal whose <c>Identity.Name</c> is <paramref name="name"/>.</summary>
    public static ClaimsPrincipal UserWithName(string name)
        => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, authenticationType: "test"));

    /// <summary>Builds an authenticated principal carrying an <c>org_id</c> claim of <paramref name="orgId"/>.</summary>
    public static ClaimsPrincipal UserWithOrg(string orgId)
        => new(new ClaimsIdentity(new[] { new Claim("org_id", orgId) }, authenticationType: "test"));

    /// <summary>Builds an unauthenticated principal whose <c>Identity.Name</c> is <see langword="null"/>.</summary>
    public static ClaimsPrincipal Anonymous()
        => new(new ClaimsIdentity());

    /// <summary>Builds a principal that carries no identity at all (<c>Identity</c> is <see langword="null"/>).</summary>
    public static ClaimsPrincipal WithoutIdentity()
        => new();
}
