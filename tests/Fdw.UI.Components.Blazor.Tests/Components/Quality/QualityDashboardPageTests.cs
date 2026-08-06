using Bunit;
using Fdw.Services.Quality.Clients.Models;
using Fdw.Services.Quality.Components.QualityDashboard;
using Fdw.UI.Components.Blazor.Tests.ObsInfra;
using QualityDashboardPage = Fdw.Services.Quality.UI.Pages.Pages.Quality.Dashboard;

namespace Fdw.UI.Components.Blazor.Tests.Components.Quality;

/// <summary>
/// Component tests for the FDW Quality Dashboard page (<c>Pages/Quality/Dashboard.razor</c>).
/// Relocated from reference-ui's QualityDashboardPageTests; the page renders directly with its
/// provider stubbed by a seeded <see cref="QualityDashboardContext"/>. Assertions target the
/// CURRENT markup (the reference-ui loading-spinner assertion used the wrong class). Covers the
/// loading / unavailable / error branches, the stats cards, the health bar (TotalRules &gt; 0 vs
/// == 0), the recent-executions table (any vs none), and the Refresh action.
/// </summary>
[Trait("Category", "Ui")]
public sealed class QualityDashboardPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void SwapProvider(QualityDashboardContext? seed = null) =>
        _ctx.ComponentFactories.Add(new ProviderFactory<QualityDashboardProvider, QualityDashboardContext>(seed));

    [Fact]
    public void RendersLoadingSpinnerWhenLoadingAndNoDashboard()
    {
        SwapProvider(new QualityDashboardContext { IsLoading = true });
        var cut = _ctx.Render<QualityDashboardPage>();
        cut.Find(".loadwrap .spin").ShouldNotBeNull();
    }

    [Fact]
    public void RendersUnavailableCardWhenDashboardNullAndNotLoading()
    {
        SwapProvider(new QualityDashboardContext());
        var cut = _ctx.Render<QualityDashboardPage>();
        cut.Markup.ShouldContain("Quality dashboard data unavailable");
    }

    [Fact]
    public void RendersErrorBannerWhenErrorPresent()
    {
        SwapProvider(new QualityDashboardContext { ErrorMessage = "dashboard down" });
        var cut = _ctx.Render<QualityDashboardPage>();
        cut.Markup.ShouldContain("dashboard down");
    }

    [Fact]
    public void RendersStatsCardsWhenDashboardPresent()
    {
        SwapProvider(new QualityDashboardContext
        {
            Dashboard = new QualityDashboardPayload { TotalRules = 10, PassingRules = 7, FailingRules = 3 },
        });
        var cut = _ctx.Render<QualityDashboardPage>();
        cut.Markup.ShouldContain("Total Rules");
        cut.Markup.ShouldContain("Passing");
        cut.Markup.ShouldContain("Failing");
        cut.Markup.ShouldContain("10");
        cut.Markup.ShouldContain("7");
        cut.Markup.ShouldContain("3");
    }

    [Fact]
    public void RendersHealthBarWhenTotalRulesPositive()
    {
        SwapProvider(new QualityDashboardContext
        {
            Dashboard = new QualityDashboardPayload { TotalRules = 4, PassingRules = 2, FailingRules = 2 },
        });
        var cut = _ctx.Render<QualityDashboardPage>();
        cut.Markup.ShouldContain("Quality Health");
        cut.Markup.ShouldContain("50% passing");
    }

    [Fact]
    public void DoesNotRenderHealthBarWhenTotalRulesZero()
    {
        SwapProvider(new QualityDashboardContext
        {
            Dashboard = new QualityDashboardPayload { TotalRules = 0, PassingRules = 0, FailingRules = 0 },
        });
        var cut = _ctx.Render<QualityDashboardPage>();
        cut.Markup.ShouldNotContain("Quality Health");
    }

    [Fact]
    public void RendersRecentExecutionsTableWhenAny()
    {
        SwapProvider(new QualityDashboardContext
        {
            Dashboard = new QualityDashboardPayload { TotalRules = 1, PassingRules = 1, FailingRules = 0 },
            RecentExecutions = [new() { Id = Guid.NewGuid(), Name = "FreshnessRule", Description = "recent", IsEnabled = true }],
        });
        var cut = _ctx.Render<QualityDashboardPage>();
        cut.Markup.ShouldContain("Rules Overview");
        cut.Markup.ShouldContain("FreshnessRule");
    }

    [Fact]
    public void DoesNotRenderRecentExecutionsWhenEmpty()
    {
        SwapProvider(new QualityDashboardContext
        {
            Dashboard = new QualityDashboardPayload { TotalRules = 1, PassingRules = 1, FailingRules = 0 },
            RecentExecutions = [],
        });
        var cut = _ctx.Render<QualityDashboardPage>();
        cut.Markup.ShouldNotContain("Rules Overview");
    }

    [Fact]
    public async Task RefreshInvokesOnRefresh()
    {
        var calls = 0;
        SwapProvider(new QualityDashboardContext { OnRefresh = () => { calls++; return Task.CompletedTask; } });
        var cut = _ctx.Render<QualityDashboardPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();
        await Task.Yield();
        calls.ShouldBe(1);
    }

    public void Dispose() => _ctx.Dispose();
}
