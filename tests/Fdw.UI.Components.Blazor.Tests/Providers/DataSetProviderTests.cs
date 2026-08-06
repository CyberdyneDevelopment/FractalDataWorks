using Bunit;
using Fdw.Services.Data.Clients.Models;
using Fdw.Data.Components.DataSets;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="DataSetProvider"/> headless component.
/// Uses MockHttpHandler because DataSetApiClient is created internally via IHttpClientFactory.
/// </summary>
[Trait("Category", "Ui")]
public sealed class DataSetProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public DataSetProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<DataSetProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<DataSetProvider>>(NullLogger<DataSetProvider>.Instance);

        return _ctx.Render<DataSetProvider>();
    }

    private static DataSetContext GetContext(IRenderedComponent<DataSetProvider> component)
    {
        var field = typeof(DataSetProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (DataSetContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadDataSets_LoadsItems()
    {
        var items = new List<DataSetSummaryPayload>
        {
            new() { Name = "SalesData", Category = "Standard" },
            new() { Name = "HrData", Category = "Standard" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("datasets", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadDataSets();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.DataSets.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadDataSets_EmptyList_ReturnsEmpty()
    {
        var handler = new MockHttpHandler()
            .RespondWith("datasets", new List<DataSetSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadDataSets();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.DataSets.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadDataSets_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("datasets");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadDataSets();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.DataSets.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Detail Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadDataSet_LoadsCurrentDataSet()
    {
        var detail = new DataSetDetailPayload { Name = "SalesData" };

        var handler = new MockHttpHandler()
            .RespondWith("datasets/SalesData", detail);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadDataSet("SalesData");
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.CurrentDataSet.ShouldNotBeNull();
        resultCtx.CurrentDataSet!.Name.ShouldBe("SalesData");
    }

    // ── Delete Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDeleteDataSet_Success_RefreshesList()
    {
        var handler = new MockHttpHandler()
            .RespondOk("datasets/SalesData")
            .RespondWith("datasets", new List<DataSetSummaryPayload>());

        var component = RenderWithHandler(handler);

        bool deleted = false;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            deleted = await ctx.OnDeleteDataSet("SalesData");
        });

        deleted.ShouldBeTrue();
        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.DataSets.Count.ShouldBe(0);
    }

    public void Dispose() => _ctx.Dispose();
}
