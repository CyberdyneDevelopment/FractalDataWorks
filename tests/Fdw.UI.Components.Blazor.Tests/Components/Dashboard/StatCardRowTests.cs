using Bunit;
using Fdw.Dashboard.UI.Components;
using Fdw.Services.Connections.Components.Dashboard;
using Fdw.Services.Pipelines.Components.Dashboard;
using Fdw.Services.Scheduling.Components.Dashboard;

namespace Fdw.UI.Components.Blazor.Tests.Components.Dashboard;

/// <summary>
/// Component tests for the FDW <see cref="StatCardRow"/> dashboard component. Relocated from
/// reference-ui's StatCardRowTests, which asserted these branches through the hosted Home page;
/// here they run directly against the component with seeded dashboard contexts. Covers the
/// loading-placeholder branch (any context IsLoading renders "...") and the resolved-count branch.
/// </summary>
[Trait("Category", "Ui")]
public sealed class StatCardRowTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<StatCardRow> RenderRow(
        ConnectionDashboardContext? conn = null,
        PipelineDashboardContext? pipe = null,
        ScheduleDashboardContext? sched = null) =>
        _ctx.Render<StatCardRow>(p => p
            .Add(x => x.ConnectionContext, conn ?? new ConnectionDashboardContext())
            .Add(x => x.PipelineContext, pipe ?? new PipelineDashboardContext())
            .Add(x => x.ScheduleContext, sched ?? new ScheduleDashboardContext()));

    [Fact]
    public void RendersResolvedCountsWhenNotLoading()
    {
        var cut = RenderRow(
            new ConnectionDashboardContext { TotalConnections = 3 },
            new PipelineDashboardContext { TotalPipelines = 8 },
            new ScheduleDashboardContext { ActiveSchedules = 5 });
        cut.Markup.ShouldContain("Total Pipelines");
        cut.Markup.ShouldContain("8");
        cut.Markup.ShouldContain("3");
        cut.Markup.ShouldContain("5");
        cut.Markup.ShouldNotContain("...");
    }

    [Fact]
    public void RendersLoadingPlaceholdersWhenConnectionLoading()
    {
        var cut = RenderRow(conn: new ConnectionDashboardContext { IsLoading = true });
        cut.Markup.ShouldContain("Active Pipelines");
        cut.Markup.ShouldContain("...");
    }

    [Fact]
    public void RendersLoadingPlaceholdersWhenPipelineLoading()
    {
        var cut = RenderRow(pipe: new PipelineDashboardContext { IsLoading = true });
        cut.Markup.ShouldContain("...");
    }

    [Fact]
    public void RendersLoadingPlaceholdersWhenScheduleLoading()
    {
        var cut = RenderRow(sched: new ScheduleDashboardContext { IsLoading = true });
        cut.Markup.ShouldContain("...");
    }

    public void Dispose() => _ctx.Dispose();
}
