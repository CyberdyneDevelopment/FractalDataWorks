using Bunit;
using Fdw.Dashboard.UI.Components;
using Fdw.Operations.Clients.Models;
using Fdw.Operations.Components.Dashboard;
using Fdw.Services.Abstractions.Health;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Services.Connections.Components.Dashboard;
using Fdw.Services.Pipelines.Components.Dashboard;
using Fdw.Services.Scheduling.Components.Dashboard;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.Web.Analytics.Components.Dashboard;
using Fdw.Web.Analytics.Components.Health.Dashboard;

namespace Fdw.UI.Components.Blazor.Tests.Components.Dashboard;

/// <summary>
/// Component tests for the composite FDW <see cref="DashboardLayout"/>. Relocated from reference-ui's
/// DashboardLayoutTests, which rendered this component through the hosted Home page; here it renders
/// directly against seeded contexts and asserts the CURRENT markup (the reference-ui assertions were
/// stale Tailwind colour classes). Covers: header, health-gauge block present/absent and the
/// services-present vs empty-fallback gauge branches, throughput chart (bars + error colouring) vs
/// empty placeholder, recent-activity list vs empty, and the activity status-colour switch.
/// </summary>
[Trait("Category", "Ui")]
public sealed class DashboardLayoutTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private static SystemHealthSnapshot Health(params ServiceHealthSnapshot[] services) => new()
    {
        OverallStatus = new HealthyState(),
        Services = services,
        Timestamp = DateTimeOffset.Now,
    };

    private IRenderedComponent<DashboardLayout> RenderLayout(
        HealthDashboardContext? health = null,
        AnalyticsDashboardContext? analytics = null,
        OperationsDashboardContext? operations = null,
        ConnectionDashboardContext? conn = null,
        PipelineDashboardContext? pipe = null,
        ScheduleDashboardContext? sched = null) =>
        _ctx.Render<DashboardLayout>(p => p
            .Add(x => x.HealthContext, health ?? new HealthDashboardContext())
            .Add(x => x.AnalyticsContext, analytics ?? new AnalyticsDashboardContext())
            .Add(x => x.OperationsContext, operations ?? new OperationsDashboardContext())
            .Add(x => x.ConnectionContext, conn ?? new ConnectionDashboardContext())
            .Add(x => x.PipelineContext, pipe ?? new PipelineDashboardContext())
            .Add(x => x.ScheduleContext, sched ?? new ScheduleDashboardContext()));

    [Fact]
    public void RendersHeader()
    {
        var cut = RenderLayout();
        cut.Markup.ShouldContain("System Overview");
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void OmitsHealthBlockWhenNoSystemHealth()
    {
        var cut = RenderLayout();
        cut.Markup.ShouldNotContain("System Health");
    }

    [Fact]
    public void RendersHealthBlockWithGaugesWhenServicesPresent()
    {
        var cut = RenderLayout(health: new HealthDashboardContext
        {
            SystemHealth = Health(new ServiceHealthSnapshot { Name = "api", Status = new HealthyState(), ResponseTimeMs = 100 }),
        });
        cut.Markup.ShouldContain("System Health");
        cut.Markup.ShouldContain("Avg Response");
        cut.Markup.ShouldContain("Req/s");
    }

    [Fact]
    public void RendersHealthBlockFallbackGaugesWhenNoServices()
    {
        var cut = RenderLayout(health: new HealthDashboardContext { SystemHealth = Health() });
        cut.Markup.ShouldContain("System Health");
        // Zero-value fallback gauges still render their labels.
        cut.Markup.ShouldContain("Avg Response");
    }

    [Fact]
    public void RendersThroughputBarsWhenTimeSeriesPresent()
    {
        var cut = RenderLayout(analytics: new AnalyticsDashboardContext
        {
            AnalyticsData = new AnalyticsResponse
            {
                TimeSeries =
                [
                    new TimeSeriesDataPoint { Timestamp = DateTimeOffset.Now.AddHours(-1), ExecutionCount = 10, ErrorCount = 0 },
                    new TimeSeriesDataPoint { Timestamp = DateTimeOffset.Now, ExecutionCount = 5, ErrorCount = 2 },
                ],
            },
        });
        cut.Markup.ShouldContain("Throughput Analysis");
        // The error-bearing bar uses the signal colour; the clean bar uses the glacier colour.
        cut.Markup.ShouldContain("var(--signal)");
        cut.Markup.ShouldContain("var(--glacier)");
    }

    [Fact]
    public void RendersThroughputPlaceholderWhenNoTimeSeries()
    {
        var cut = RenderLayout();
        cut.Markup.ShouldContain("No throughput data available");
    }

    [Fact]
    public void RendersRecentActivityWhenAny()
    {
        var cut = RenderLayout(operations: new OperationsDashboardContext
        {
            Activities = [new ActivityEntryPayload { Title = "PipelineRan", Severity = "success", Timestamp = DateTimeOffset.Now }],
        });
        cut.Markup.ShouldContain("PipelineRan");
        cut.Markup.ShouldContain("dot-green"); // success status colour
    }

    [Fact]
    public void RendersNoActivityWhenEmpty()
    {
        var cut = RenderLayout();
        cut.Markup.ShouldContain("No recent activity detected");
    }

    [Theory]
    [InlineData("error", "dot-red")]
    [InlineData("warning", "dot-amber")]
    [InlineData("info", "dot-glacier")]
    public void RendersActivityStatusColor(string severity, string colorClass)
    {
        var cut = RenderLayout(operations: new OperationsDashboardContext
        {
            Activities = [new ActivityEntryPayload { Title = "X", Severity = severity, Timestamp = DateTimeOffset.Now }],
        });
        cut.Markup.ShouldContain(colorClass);
    }

    [Fact]
    public void RendersStatCardRowWithCounts()
    {
        var cut = RenderLayout(
            conn: new ConnectionDashboardContext { TotalConnections = 4 },
            pipe: new PipelineDashboardContext { TotalPipelines = 9 },
            sched: new ScheduleDashboardContext { ActiveSchedules = 2 });
        cut.Markup.ShouldContain("Total Pipelines");
        cut.Markup.ShouldContain("9");
        cut.Markup.ShouldContain("4");
        cut.Markup.ShouldContain("2");
    }

    public void Dispose() => _ctx.Dispose();
}
