using System;
using System.Collections.Generic;
using ApexCharts;
using Bunit;
using TestContext = Bunit.BunitContext;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Renderers.ApexChartsRender;
using Fdw.UI.Charts.Blazor.Tests.Fakes;
using Shouldly;
using Xunit;

namespace Fdw.UI.Charts.Blazor.Tests;

/// <summary>
/// bUnit tests for <c>ApexChartsRenderer</c>.
/// </summary>
public sealed class ApexChartsRendererTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────────

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddApexCharts();
        return ctx;
    }

    // ── 4. Renders chart element for a simple Bar model + rows ────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void RendersChartHostDivForBarModelWithRows()
    {
        // Arrange
        using var ctx = CreateContext();

        var model = new FakeChartModel
        {
            Title     = "Sales by Region",
            ChartType = ChartTypes.ByName("Bar"),
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
        var cut = ctx.Render<ApexChartsRenderer>(p => p
            .Add(r => r.Model, model)
            .Add(r => r.Rows, rows));

        // Assert: the renderer mounts with the fdw-chart-host wrapper div, no error shown.
        var errorElements = cut.FindAll("[style*='ef4444']");
        errorElements.ShouldBeEmpty("No error message should appear for a valid Bar model");

        var chartDivs = cut.FindAll(".fdw-chart-host");
        chartDivs.ShouldNotBeEmpty(
            "Expected the fdw-chart-host wrapper div indicating the ApexChart component was rendered");
    }

    // ── 5. Unsupported chart type shows error, not exception ─────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void UnsupportedChartTypeRendersErrorMessage()
    {
        // Arrange
        using var ctx = CreateContext();

        var model = new FakeChartModel
        {
            // Geo is not in the ApexCharts strategy map — dispatcher returns null.
            ChartType = ChartTypes.ByName("Geo"),
        };

        // Act
        var cut = ctx.Render<ApexChartsRenderer>(p => p
            .Add(r => r.Model, model)
            .Add(r => r.Rows, new List<IReadOnlyDictionary<string, object?>>()));

        // Assert: an error element is rendered; no exception thrown.
        var errorEl = cut.FindAll("[style*='ef4444']");
        errorEl.ShouldNotBeEmpty(
            "Expected an error message element when chart type is unsupported by this renderer");
    }
}
