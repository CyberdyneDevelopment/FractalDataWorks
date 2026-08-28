using Bunit;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Connections.Components.Connections;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="ConnectionProvider"/> headless component.
/// Uses MockHttpHandler because ConnectionApiClient and ConfigurationApiClient are created
/// internally via IHttpClientFactory.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ConnectionProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public ConnectionProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<ConnectionProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<ConnectionProvider>>(NullLogger<ConnectionProvider>.Instance);

        return _ctx.Render<ConnectionProvider>();
    }

    private static ConnectionContext GetContext(IRenderedComponent<ConnectionProvider> component)
    {
        var field = typeof(ConnectionProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (ConnectionContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_LoadsConnections()
    {
        var items = new List<ConnectionPayload>
        {
            new() { Name = "DevDb", ConnectionType = "MsSql" },
            new() { Name = "ProdDb", ConnectionType = "MsSql" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("connections", items)
            .RespondWith("configuration/types", new List<object>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Connections.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_EmptyList_ReturnsEmpty()
    {
        var handler = new MockHttpHandler()
            .RespondWith("connections", new List<ConnectionPayload>())
            .RespondWith("configuration/types", new List<object>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Connections.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("connections")
            .RespondWith("configuration/types", new List<object>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Connections.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Filter Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnSearchStringChanged_FiltersConnections()
    {
        var items = new List<ConnectionPayload>
        {
            new() { Name = "DevDb", ConnectionType = "MsSql" },
            new() { Name = "ProdPostgres", ConnectionType = "PostgreSql" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("connections", items)
            .RespondWith("configuration/types", new List<object>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        await component.InvokeAsync(() =>
        {
            var ctx = GetContext(component);
            ctx.OnSearchStringChanged("Prod");
        });

        var resultCtx = GetContext(component);
        resultCtx.FilteredConnections.Count().ShouldBe(1);
        resultCtx.FilteredConnections.First().Name.ShouldBe("ProdPostgres");
    }

    public void Dispose() => _ctx.Dispose();
}
