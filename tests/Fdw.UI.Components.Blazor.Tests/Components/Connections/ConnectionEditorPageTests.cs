using Bunit;
using Fdw.Operations.Clients.Models;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Connections.Components.Connections;
using Fdw.Services.Connections.UI.Pages.Pages;
using Fdw.UI.Components.Blazor.Tests.ConnInfra;

namespace Fdw.UI.Components.Blazor.Tests.Components.Connections;

/// <summary>
/// Tests for the <see cref="ConnectionEditor"/> FDW page (the legacy
/// "/connections/{Name}/configure" form). Relocated from reference-ui's ConnectionEditorPageTests:
/// the deep header/name-input/type-select/save-create-update assertions were reframed in the app
/// to a host smoke, and the equivalent (or stronger) coverage now runs here against the FDW page
/// rendered through a stubbed <see cref="ConnectionProvider"/>.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ConnectionEditorPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void Swap(ConnectionContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<ConnectionProvider, ConnectionContext>(seed));

    private static ConnectionTypePayload Type(string name) => new() { Name = name, Category = name };

    [Fact]
    public void NewRendersNewConnectionHeaderAndEnabledNameInput()
    {
        Swap(new ConnectionContext { ConnectionTypes = [Type("MsSql")] });
        var cut = _ctx.Render<ConnectionEditor>();
        cut.Markup.ShouldContain("New Connection");
        cut.Find("input[placeholder='Connection name']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void EditRendersEditHeaderAndDisabledNameInput()
    {
        Swap(new ConnectionContext { ConnectionTypes = [Type("MsSql")] });
        var cut = _ctx.Render<ConnectionEditor>(p => p.Add(x => x.Name, "PROD"));
        cut.Markup.ShouldContain("Edit Connection");
        cut.Find("input[placeholder='Connection name']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void TypeSelectRendersWhenTypesPresent()
    {
        Swap(new ConnectionContext { ConnectionTypes = [Type("MsSql"), Type("Http")] });
        var cut = _ctx.Render<ConnectionEditor>();
        cut.Markup.ShouldContain("Connection Type");
        cut.Markup.ShouldContain("MsSql");
        cut.Markup.ShouldContain("Http");
    }

    [Fact]
    public void TypeSelectHiddenWhenNoTypes()
    {
        Swap(new ConnectionContext { ConnectionTypes = [] });
        var cut = _ctx.Render<ConnectionEditor>();
        cut.Markup.ShouldNotContain("Connection Type");
    }

    [Fact]
    public async Task SaveNewWithEmptyNameDoesNotCallCreate()
    {
        var created = false;
        Swap(new ConnectionContext
        {
            ConnectionTypes = [Type("MsSql")],
            OnCreateConnection = _ => { created = true; return Task.FromResult<ConnectionDetailResponse?>(null); }
        });
        var cut = _ctx.Render<ConnectionEditor>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Save", StringComparison.Ordinal)).Click();
        await Task.Yield();
        created.ShouldBeFalse(); // name empty => early return
    }

    [Fact]
    public async Task SaveNewWithNameInvokesCreate()
    {
        CreateConnectionClientRequest? captured = null;
        Swap(new ConnectionContext
        {
            ConnectionTypes = [Type("MsSql")],
            OnCreateConnection = req => { captured = req; return Task.FromResult<ConnectionDetailResponse?>(new ConnectionDetailResponse { Name = req.Name }); }
        });
        var cut = _ctx.Render<ConnectionEditor>();
        cut.Find("input[placeholder='Connection name']").Change("NEWCONN");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save", StringComparison.Ordinal)).Click();
        await Task.Yield();
        captured.ShouldNotBeNull();
        captured!.Name.ShouldBe("NEWCONN");
    }

    [Fact]
    public void SaveNewCreateReturnsNullShowsError()
    {
        Swap(new ConnectionContext
        {
            ConnectionTypes = [Type("MsSql")],
            OnCreateConnection = _ => Task.FromResult<ConnectionDetailResponse?>(null)
        });
        var cut = _ctx.Render<ConnectionEditor>();
        cut.Find("input[placeholder='Connection name']").Change("X");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to create connection"));
    }

    [Fact]
    public async Task SaveEditInvokesUpdate()
    {
        string? updatedName = null;
        Swap(new ConnectionContext
        {
            ConnectionTypes = [Type("MsSql")],
            OnUpdateConnection = (name, _) => { updatedName = name; return Task.FromResult<ConnectionDetailResponse?>(new ConnectionDetailResponse { Name = name }); }
        });
        var cut = _ctx.Render<ConnectionEditor>(p => p.Add(x => x.Name, "PROD"));
        cut.Find("input[placeholder='Connection name']").Change("PROD");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save", StringComparison.Ordinal)).Click();
        await Task.Yield();
        updatedName.ShouldBe("PROD");
    }

    public void Dispose() => _ctx.Dispose();
}
