using System.Collections.Generic;
using Bunit;
using TestContext = Bunit.BunitContext;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Renderers.RadzenCharts;
using Fdw.UI.Charts.Blazor.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using Shouldly;
using Xunit;

namespace Fdw.UI.Charts.Blazor.Tests;

/// <summary>
/// bUnit tests for <c>RadzenChartsRenderer</c>.
/// </summary>
public sealed class RadzenChartsRendererTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────────

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddRadzenComponents();
        return ctx;
    }

    // ── Dispatch contract for a simple Bar model + rows ───────────────────────────

    /// <summary>
    /// Verifies that a valid Bar model dispatches through the strategy map to a Radzen
    /// configuration carrying a non-null series fragment and the model's display options.
    /// </summary>
    /// <remarks>
    /// Why this is not a bUnit render test: a live <c>RadzenChart</c> measures its container
    /// via the browser DOM in <c>OnAfterRenderAsync</c>; under bUnit there is no DOM and the
    /// vendor component throws inside its own post-render layout. Driving the vendor chart to
    /// completion would test Radzen, not this renderer. The renderer's own contract is the
    /// strategy-map dispatch (chart type → configuration + series fragment), which is fully
    /// deterministic and asserted directly here. The error/empty paths (which never mount a live
    /// chart) remain bUnit render tests below.
    /// </remarks>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void BarModelDispatchesToConfigurationWithSeriesFragment()
    {
        // Arrange
        var model = new FakeChartModel
        {
            Title      = "Sales by Region",
            ChartType  = ChartTypes.ByName("Bar"),
            ShowLegend = true,
            Encodings =
            [
                new ChartEncoding(ChartEncodingRoles.ByName("X"), "Region"),
                new ChartEncoding(ChartEncodingRoles.ByName("Y"), "Sales"),
            ],
        };

        var rows = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Region"] = "North", ["Sales"] = 1200m },
            new Dictionary<string, object?> { ["Region"] = "South", ["Sales"] = 850m  },
            new Dictionary<string, object?> { ["Region"] = "West",  ["Sales"] = 970m  },
        };

        // Act
        var strategy = RadzenChartStrategyMap.For("Bar");
        strategy.ShouldNotBeNull("Bar must be a supported chart type in the Radzen strategy map");
        var config = strategy(model, rows);

        // Assert: dispatch produced a renderable configuration carrying the model's options
        // and a non-null series fragment (the column series the renderer mounts inside RadzenChart).
        config.ShouldNotBeNull();
        config.Title.ShouldBe("Sales by Region");
        config.ShowLegend.ShouldBeTrue();
        config.SeriesFragment.ShouldNotBeNull(
            "A Bar model with rows must yield a series fragment for the renderer to mount");
    }

    // ── Unsupported chart type shows error, not exception ─────────────────────────

    /// <summary>
    /// Verifies that an unsupported chart type renders an error element rather than
    /// throwing an exception.
    /// </summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void UnsupportedChartTypeRendersErrorMessage()
    {
        // Arrange
        using var ctx = CreateContext();

        var model = new FakeChartModel
        {
            // Geo is not in the Radzen strategy map — dispatcher returns null.
            ChartType = ChartTypes.ByName("Geo"),
        };

        // Act
        var cut = ctx.Render<RadzenChartsRenderer>(p => p
            .Add(r => r.Model, model)
            .Add(r => r.Rows, new List<IReadOnlyDictionary<string, object?>>()));

        // Assert: an error element is rendered; no exception thrown.
        var errorEl = cut.FindAll("[style*='ef4444']");
        errorEl.ShouldNotBeEmpty(
            "Expected an error message element when chart type is unsupported by this renderer");
    }
}
