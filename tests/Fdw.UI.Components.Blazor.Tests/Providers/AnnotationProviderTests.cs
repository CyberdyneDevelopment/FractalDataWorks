using Bunit;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.Data.Components.Annotations;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="AnnotationProvider"/> headless component.
/// Uses MockHttpHandler because CatalogApiClient is sealed.
/// </summary>
[Trait("Category", "Ui")]
public sealed class AnnotationProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public AnnotationProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<AnnotationProvider> RenderWithHandler(MockHttpHandler handler, string dataSetName = "TestDataSet")
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<AnnotationProvider>>(NullLogger<AnnotationProvider>.Instance);

        return _ctx.Render<AnnotationProvider>(p => p
            .Add(x => x.DataSetName, dataSetName));
    }

    private static AnnotationContext GetContext(IRenderedComponent<AnnotationProvider> component)
    {
        var field = typeof(AnnotationProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (AnnotationContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnRefresh_LoadsAnnotations()
    {
        var annotations = new List<DataSetAnnotationPayload>
        {
            new() { DataSetName = "TestDataSet", Classification = "Internal" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("annotations", annotations);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Annotations.Count.ShouldBe(1);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnRefresh_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("annotations");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
        resultCtx.Annotations.Count.ShouldBe(0);
    }

    // ── Create Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnCreate_Success_RefreshesList()
    {
        var created = new DataSetAnnotationPayload { DataSetName = "TestDataSet", Owner = "admin" };
        var refreshed = new List<DataSetAnnotationPayload> { created };

        var handler = new MockHttpHandler()
            .RespondWith("annotations", refreshed);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnCreate(new CreateAnnotationRequest { Owner = "admin" });
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    // ── Delete Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDelete_Success_RefreshesList()
    {
        var handler = new MockHttpHandler()
            .RespondWith("annotations", new List<DataSetAnnotationPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnDelete(Guid.NewGuid());
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    // ── Resolve Tests ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnResolve_Success_RefreshesList()
    {
        var handler = new MockHttpHandler()
            .RespondWith("annotations", new List<DataSetAnnotationPayload>())
            .RespondWith("resolve", new DataSetAnnotationPayload { DataSetName = "TestDataSet" });

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnResolve(Guid.NewGuid());
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    public void Dispose() => _ctx.Dispose();
}
