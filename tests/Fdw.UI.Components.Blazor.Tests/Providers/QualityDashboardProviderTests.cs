using Bunit;
using Fdw.Services.Quality.Clients.Models;
using Fdw.Services.Quality.Components.QualityDashboard;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="QualityDashboardProvider"/> headless component.
/// Uses MockHttpHandler because QualityApiClient is sealed.
/// </summary>
[Trait("Category", "Ui")]
public sealed class QualityDashboardProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public QualityDashboardProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<QualityDashboardProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<QualityDashboardProvider>>(NullLogger<QualityDashboardProvider>.Instance);

        return _ctx.Render<QualityDashboardProvider>();
    }

    private static QualityDashboardContext GetContext(IRenderedComponent<QualityDashboardProvider> component)
    {
        var field = typeof(QualityDashboardProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (QualityDashboardContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnRefresh_LoadsDashboardAndRules()
    {
        var dashboard = new QualityDashboardPayload { TotalRules = 10, PassingRules = 8, FailingRules = 2 };
        var rules = new List<QualityRuleSummaryPayload>
        {
            new() { Id = Guid.NewGuid(), Name = "Rule1", IsEnabled = true }
        };

        var handler = new MockHttpHandler()
            .RespondWith("quality/dashboard", dashboard)
            .RespondWith("quality/rules", rules);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Dashboard.ShouldNotBeNull();
        resultCtx.Dashboard!.TotalRules.ShouldBe(10);
        resultCtx.RecentExecutions.Count.ShouldBe(1);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnRefresh_DashboardApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("quality/dashboard");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Dashboard.ShouldBeNull();
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnRefresh_RulesFailure_DashboardStillLoaded()
    {
        var dashboard = new QualityDashboardPayload { TotalRules = 5, PassingRules = 5, FailingRules = 0 };

        var handler = new MockHttpHandler()
            .RespondWith("quality/dashboard", dashboard)
            .RespondError("quality/rules");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Dashboard.ShouldNotBeNull();
        resultCtx.RecentExecutions.Count.ShouldBe(0);
    }

    public void Dispose() => _ctx.Dispose();
}
