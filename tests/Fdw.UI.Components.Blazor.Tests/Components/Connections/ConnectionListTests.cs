using Bunit;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Connections.Components.Connections;
using Fdw.Services.Connections.UI.Components;

namespace Fdw.UI.Components.Blazor.Tests.Components.Connections;

/// <summary>
/// Component tests for the <see cref="ConnectionList"/> FDW UI component. Relocated from
/// reference-ui's ConnectionsPageTests, which asserted these behaviours through the hosted page;
/// here they run directly against the component with a seeded <see cref="ConnectionContext"/>.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ConnectionListTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private static ConnectionPayload Conn(string name, string type = "MsSql", bool? healthy = true) =>
        new() { Id = Guid.NewGuid(), Name = name, ConnectionType = type, LastTestSuccess = healthy };

    private IRenderedComponent<ConnectionList> Render(ConnectionContext context, Action<ComponentParameterCollectionBuilder<ConnectionList>>? extra = null) =>
        _ctx.Render<ConnectionList>(p =>
        {
            p.Add(c => c.Context, context);
            extra?.Invoke(p);
        });

    [Fact]
    public void RendersRowsForEachConnectionInContext()
    {
        var list = new List<ConnectionPayload> { Conn("Alpha"), Conn("Beta") };
        var cut = Render(new ConnectionContext { Connections = list, FilteredConnections = list });
        cut.Markup.ShouldContain("Alpha");
        cut.Markup.ShouldContain("Beta");
    }

    [Fact]
    public void RendersEmptyStateWhenNoConnections()
    {
        var cut = Render(new ConnectionContext());
        cut.Markup.ShouldContain("No connections configured");
    }

    [Fact]
    public void RendersLoadingBadgeWhenLoadingAndNoFilteredConnections()
    {
        var cut = Render(new ConnectionContext { IsLoading = true });
        cut.FindAll(".badge.b-run").Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void RendersNewConnectionButton()
    {
        var cut = Render(new ConnectionContext());
        cut.FindAll("button").Any(b => b.TextContent.Contains("New Connection", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void ClickingNewConnectionInvokesOnNewConnectionCallback()
    {
        var invoked = false;
        var cut = Render(new ConnectionContext(), p => p.Add(c => c.OnNewConnection,
            Microsoft.AspNetCore.Components.EventCallback.Factory.Create(this, () => invoked = true)));
        cut.FindAll("button").First(b => b.TextContent.Contains("New Connection", StringComparison.Ordinal)).Click();
        invoked.ShouldBeTrue();
    }

    [Fact]
    public void HealthyConnectionRendersHealthyBadge()
    {
        var list = new List<ConnectionPayload> { Conn("Alpha", healthy: true) };
        var cut = Render(new ConnectionContext { Connections = list, FilteredConnections = list });
        cut.Markup.ShouldContain("Healthy");
    }

    [Fact]
    public void UnhealthyConnectionRendersUnhealthyBadge()
    {
        var list = new List<ConnectionPayload> { Conn("Beta", healthy: false) };
        var cut = Render(new ConnectionContext { Connections = list, FilteredConnections = list });
        cut.Markup.ShouldContain("Unhealthy");
    }

    [Fact]
    public void NeverTestedConnectionRendersUnknownBadge()
    {
        var list = new List<ConnectionPayload> { Conn("Gamma", healthy: null) };
        var cut = Render(new ConnectionContext { Connections = list, FilteredConnections = list });
        cut.Markup.ShouldContain("Unknown");
        cut.FindAll(".badge.b-idle").Count.ShouldBeGreaterThan(0);
    }

    public void Dispose() => _ctx.Dispose();
}
