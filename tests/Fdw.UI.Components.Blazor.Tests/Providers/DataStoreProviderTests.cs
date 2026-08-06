using Bunit;
using Fdw.Services.Data.Clients.Models;
using Fdw.Data.Components.DataStores;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="DataStoreProvider"/> headless component.
/// Uses MockHttpHandler because DataStoreApiClient is created internally via IHttpClientFactory.
/// </summary>
[Trait("Category", "Ui")]
public sealed class DataStoreProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public DataStoreProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<DataStoreProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<DataStoreProvider>>(NullLogger<DataStoreProvider>.Instance);

        return _ctx.Render<DataStoreProvider>();
    }

    private static DataStoreContext GetContext(IRenderedComponent<DataStoreProvider> component)
    {
        var field = typeof(DataStoreProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (DataStoreContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadDataStores_LoadsItems()
    {
        var items = new List<DataStoreSummaryPayload>
        {
            new() { Name = "SalesStore", ConnectionName = "DevDb" },
            new() { Name = "HrStore", ConnectionName = "DevDb" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("datastores", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadDataStores();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.DataStores.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadDataStores_EmptyList_ReturnsEmpty()
    {
        var handler = new MockHttpHandler()
            .RespondWith("datastores", new List<DataStoreSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadDataStores();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.DataStores.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadDataStores_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("datastores");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadDataStores();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.DataStores.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Detail Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnGetDataStoreDetails_Success_ReturnsDetail()
    {
        var detail = new DataStoreDetailPayload { Name = "SalesStore" };

        var handler = new MockHttpHandler()
            .RespondWith("datastores/SalesStore", detail);

        var component = RenderWithHandler(handler);

        DataStoreDetailPayload? result = null;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            result = await ctx.OnGetDataStoreDetails("SalesStore");
        });

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("SalesStore");
    }

    // ── Delete Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDeleteDataStore_Success_RefreshesList()
    {
        var handler = new MockHttpHandler()
            .RespondOk("datastores/SalesStore")
            .RespondWith("datastores", new List<DataStoreSummaryPayload>());

        var component = RenderWithHandler(handler);

        bool deleted = false;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            deleted = await ctx.OnDeleteDataStore("SalesStore");
        });

        deleted.ShouldBeTrue();
        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.DataStores.Count.ShouldBe(0);
    }

    public void Dispose() => _ctx.Dispose();
}
