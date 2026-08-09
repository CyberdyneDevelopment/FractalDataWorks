using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.UI.Components.Blazor.Tests.ObsInfra;
using Fdw.Web.Analytics.Components.Health.Dashboard;
using HealthPage = Fdw.UI.Pages.Operations.Pages.HealthDashboardPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Dashboard;

/// <summary>
/// Component tests for the FDW Health Dashboard page (<c>Pages/HealthDashboard.razor</c>). Relocated
/// from reference-ui's HealthDashboardPageTests, which asserted these branches through the hosted
/// reference-ui page; here the page renders directly with its provider stubbed by a seeded
/// <see cref="HealthDashboardContext"/>. Assertions target the CURRENT markup (the reference-ui
/// assertions were stale Tailwind colour/text). Covers loading/error states, the service map,
/// per-service throughput line + high-error-rate colour, the throughput-details panel branch, and
/// uptime formatting.
/// </summary>
[Trait("Category", "Ui")]
public sealed class HealthDashboardPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void SwapProvider(HealthDashboardContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<HealthDashboardProvider, HealthDashboardContext>(seed));

    private static SystemHealthSnapshot Snapshot(bool healthy, params ServiceHealthSnapshot[] services) => new()
    {
        OverallStatus = healthy ? new HealthyState() : new UnhealthyState(),
        Services = services,
        Timestamp = DateTimeOffset.Now,
    };

    private static ServiceHealthSnapshot Service(string name, bool healthy = true, double responseMs = 120, TimeSpan? uptime = null) => new()
    {
        Name = name,
        Status = healthy ? new HealthyState() : new UnhealthyState(),
        ResponseTimeMs = responseMs,
        LastCheckAt = DateTimeOffset.Now,
        Uptime = uptime ?? TimeSpan.FromMinutes(30),
    };

    [Fact]
    public void RendersPageLandmark()
    {
        SwapProvider(new HealthDashboardContext { SystemHealth = Snapshot(true) });
        var cut = _ctx.Render<HealthPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldContain("System Health");
    }

    [Fact]
    public void RendersLoadingWhenNoSnapshot()
    {
        SwapProvider(new HealthDashboardContext { IsLoading = true });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldContain("Loading system health");
    }

    [Fact]
    public void RendersErrorWhenNoSnapshotAndNotLoading()
    {
        SwapProvider(new HealthDashboardContext { LastResult = GenericResult.Failure(new GenericMessage("health probe failed")) });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldContain("health probe failed");
    }

    [Fact]
    public void RendersServiceMapWithHealthyAndUnhealthyDots()
    {
        SwapProvider(new HealthDashboardContext
        {
            SystemHealth = Snapshot(true, Service("api"), Service("etl", healthy: false)),
        });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldContain("Service map");
        cut.Markup.ShouldContain("api");
        cut.Markup.ShouldContain("etl");
        cut.Markup.ShouldContain("var(--success)"); // healthy dot
        cut.Markup.ShouldContain("var(--signal)");  // unhealthy dot
    }

    [Fact]
    public void RendersThroughputLineWhenPresentForService()
    {
        SwapProvider(new HealthDashboardContext
        {
            SystemHealth = Snapshot(true, Service("api")),
            ServiceThroughput = new Dictionary<string, ThroughputData>(StringComparer.OrdinalIgnoreCase)
            {
                ["api"] = new() { ServiceName = "api", RequestsPerSecond = 12.3, ErrorRate = 0.01 },
            },
        });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldContain("12.3 req/s");
        cut.Markup.ShouldContain("1.0% err");
    }

    [Fact]
    public void OmitsThroughputLineWhenAbsentForService()
    {
        SwapProvider(new HealthDashboardContext { SystemHealth = Snapshot(true, Service("api")) });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldNotContain("req/s");
    }

    [Fact]
    public void HighErrorRateUsesSignalColor()
    {
        SwapProvider(new HealthDashboardContext
        {
            SystemHealth = Snapshot(true, Service("api")),
            ServiceThroughput = new Dictionary<string, ThroughputData>(StringComparer.OrdinalIgnoreCase)
            {
                ["api"] = new() { ServiceName = "api", RequestsPerSecond = 5, ErrorRate = 0.10 },
            },
        });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldContain("10.0% err");
        cut.Markup.ShouldContain("color:var(--signal)"); // ErrorRate > 0.05 branch
    }

    [Fact]
    public void RendersThroughputDetailsPanelWhenAnyThroughput()
    {
        SwapProvider(new HealthDashboardContext
        {
            SystemHealth = Snapshot(true),
            ServiceThroughput = new Dictionary<string, ThroughputData>(StringComparer.OrdinalIgnoreCase)
            {
                ["api"] = new() { ServiceName = "api", RequestsPerSecond = 1, DataPoints = [] },
            },
        });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldContain("Throughput details");
    }

    [Fact]
    public void OmitsThroughputDetailsPanelWhenNoThroughput()
    {
        SwapProvider(new HealthDashboardContext { SystemHealth = Snapshot(true) });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldNotContain("Throughput details");
    }

    [Fact]
    public void FormatsUptimeInDays()
    {
        SwapProvider(new HealthDashboardContext
        {
            SystemHealth = Snapshot(true, Service("api", uptime: TimeSpan.FromHours(50))),
        });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldContain("up 2d");
    }

    [Fact]
    public void FormatsUptimeInHours()
    {
        SwapProvider(new HealthDashboardContext
        {
            SystemHealth = Snapshot(true, Service("api", uptime: TimeSpan.FromHours(3))),
        });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldContain("up 3h");
    }

    [Fact]
    public void FormatsUptimeInMinutes()
    {
        SwapProvider(new HealthDashboardContext
        {
            SystemHealth = Snapshot(true, Service("api", uptime: TimeSpan.FromMinutes(12))),
        });
        var cut = _ctx.Render<HealthPage>();
        cut.Markup.ShouldContain("up 12m");
    }

    [Fact]
    public async Task RefreshInvokesOnRefresh()
    {
        var calls = 0;
        SwapProvider(new HealthDashboardContext { OnRefresh = () => { calls++; return Task.FromResult<IGenericResult>(GenericResult.Success()); } });
        var cut = _ctx.Render<HealthPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();
        await Task.Yield();
        calls.ShouldBe(1);
    }

    public void Dispose() => _ctx.Dispose();
}
