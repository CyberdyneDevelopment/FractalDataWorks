using System.Collections.Generic;
using Bunit;
using TestContext = Bunit.BunitContext;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Renderers.ECharts;
using Fdw.UI.Charts.Blazor.Tests.Fakes;
using Shouldly;
using Xunit;

namespace Fdw.UI.Charts.Blazor.Tests;

/// <summary>
/// bUnit tests for <c>EChartsRenderer</c>.
/// </summary>
public sealed class EChartsRendererTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────────

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    // ── Renders host div for a valid Bar model ────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void RendersChartHostDivForBarModelWithRows()
    {
        // Arrange
        using var ctx = CreateContext();

        var model = new FakeChartModel
        {
            Title     = "Revenue by Quarter",
            ChartType = ChartTypes.ByName("Bar"),
            Encodings =
            [
                new ChartEncoding(ChartEncodingRoles.ByName("X"), "Quarter"),
                new ChartEncoding(ChartEncodingRoles.ByName("Y"), "Revenue"),
            ],
        };

        var rows = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Quarter"] = "Q1", ["Revenue"] = 42000m },
            new Dictionary<string, object?> { ["Quarter"] = "Q2", ["Revenue"] = 53500m },
            new Dictionary<string, object?> { ["Quarter"] = "Q3", ["Revenue"] = 61200m },
        };

        // Act
        var cut = ctx.Render<EChartsRenderer>(p => p
            .Add(r => r.Model, model)
            .Add(r => r.Rows, rows));

        // Assert: no error element shown; the host div is present.
        var errorElements = cut.FindAll("[style*='ef4444']");
        errorElements.ShouldBeEmpty("No error message should appear for a valid Bar model");

        var chartDivs = cut.FindAll(".fdw-chart-host");
        chartDivs.ShouldNotBeEmpty(
            "Expected the fdw-chart-host wrapper div indicating the renderer mounted successfully");
    }

    // ── Unsupported chart type shows error, not exception ─────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void UnsupportedChartTypeRendersErrorMessage()
    {
        // Arrange
        using var ctx = CreateContext();

        var model = new FakeChartModel
        {
            // Kpi is not in the ECharts strategy map — dispatcher returns null.
            ChartType = ChartTypes.ByName("Kpi"),
        };

        // Act
        var cut = ctx.Render<EChartsRenderer>(p => p
            .Add(r => r.Model, model)
            .Add(r => r.Rows, new List<IReadOnlyDictionary<string, object?>>()));

        // Assert: an error element is rendered; no exception thrown.
        var errorEl = cut.FindAll("[style*='ef4444']");
        errorEl.ShouldNotBeEmpty(
            "Expected an error message element when chart type is unsupported by this renderer");
    }

    // ── Null model renders nothing without throwing ───────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void NullModelRendersNothingWithoutThrowing()
    {
        // Arrange
        using var ctx = CreateContext();

        // Act + Assert: no exception thrown when Model is null.
        // The razor @if guard returns early — no error div, no chart div.
        Should.NotThrow(() =>
        {
            var cut = ctx.Render<EChartsRenderer>(p => p
                .Add(r => r.Model, (IChartModel)null!)
                .Add(r => r.Rows, new List<IReadOnlyDictionary<string, object?>>()));

            cut.FindAll(".fdw-chart-host").ShouldBeEmpty(
                "No chart host div should appear when model is null");
            cut.FindAll("[style*='ef4444']").ShouldBeEmpty(
                "No error div should appear when model is null");
        });
    }

    // ── Strategy map resolves all declared supported chart types ──────────────────

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    [InlineData("Bar")]
    [InlineData("Line")]
    [InlineData("Area")]
    [InlineData("Pie")]
    [InlineData("Donut")]
    [InlineData("Scatter")]
    [InlineData("Heatmap")]
    [InlineData("Sankey")]
    public void StrategyMapReturnsNonNullForAllSupportedTypes(string chartTypeName)
    {
        // Act
        var strategy = EChartsStrategyMap.For(chartTypeName);

        // Assert
        strategy.ShouldNotBeNull(
            $"EChartsStrategyMap should have a strategy for '{chartTypeName}'");
    }
}
