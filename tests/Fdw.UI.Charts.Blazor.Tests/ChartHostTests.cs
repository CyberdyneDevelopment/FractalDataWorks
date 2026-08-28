using System;
using System.Collections.Generic;
using System.Linq;
using ApexCharts;
using Bunit;
using TestContext = Bunit.BunitContext;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Charts.Blazor.Host;
using Fdw.UI.Charts.Blazor.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.UI.Charts.Blazor.Tests;

/// <summary>
/// bUnit tests for <c>ChartHost</c>.
/// </summary>
public sealed class ChartHostTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────────

    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddApexCharts();
        return ctx;
    }

    // ── 1. Renderer dropdown ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void RendererDropdownListsApexCharts()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeChartModel { ChartType = ChartTypes.ByName("Bar") };

        // Act
        var cut = ctx.Render<ChartHost>(p => p
            .Add(h => h.Model, model));

        // Assert: the first select (renderer dropdown) contains an ApexCharts option.
        var options = cut.FindAll("select option");
        options.ShouldContain(
            o => string.Equals(o.GetAttribute("value"), "ApexCharts", StringComparison.Ordinal),
            "Expected an 'ApexCharts' renderer option in the renderer dropdown");
    }

    // ── 2. Chart-type dropdown filtered to renderer's supported types ─────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ChartTypeDropdownListsOnlyApexChartsCompatibleTypes()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeChartModel { ChartType = ChartTypes.ByName("Bar") };
        var descriptor = ChartRendererTypes.ByName("ApexCharts");
        descriptor.ShouldNotBe(ChartRendererTypes.NotFound,
            "ApexChartsRendererType must be registered for these tests to be meaningful");

        // Act
        var cut = ctx.Render<ChartHost>(p => p
            .Add(h => h.Model, model));

        // Assert: at least 2 selects (renderer + chart-type).
        var selects = cut.FindAll("select");
        selects.Count.ShouldBeGreaterThanOrEqualTo(2,
            "Expected at least a renderer dropdown and a chart-type dropdown");

        var chartTypeOptions = selects[1].QuerySelectorAll("option");
        chartTypeOptions.Length.ShouldBeGreaterThan(0, "Chart-type dropdown must have options");

        var renderedNames = chartTypeOptions
            .Select(o => o.GetAttribute("value"))
            .Where(v => v is not null)
            .Cast<string>()
            .ToList();

        // Every rendered chart type must be in the renderer's SupportedChartTypes list.
        foreach (var name in renderedNames)
            descriptor.SupportedChartTypes.ShouldContain(name,
                $"'{name}' is not in ApexCharts SupportedChartTypes but appeared in the dropdown");

        // Sanity: Geo must NOT appear (ApexCharts does not support it).
        renderedNames.ShouldNotContain("Geo",
            "Geo chart type must not appear in the ApexCharts chart-type dropdown");
    }

    // ── 3. DynamicComponent renders the ApexCharts renderer ───────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void DynamicComponentRendersApexChartsRenderer()
    {
        // Arrange
        using var ctx = CreateContext();
        var model = new FakeChartModel { ChartType = ChartTypes.ByName("Bar") };

        // Act
        var cut = ctx.Render<ChartHost>(p => p
            .Add(h => h.Model, model));

        // Assert: the ApexChartsRenderer's fdw-chart-host div appears in the rendered output,
        // proving DynamicComponent resolved and mounted the renderer.
        var chartDivs = cut.FindAll(".fdw-chart-host");
        chartDivs.ShouldNotBeEmpty(
            "Expected the ApexChartsRenderer to mount with its fdw-chart-host wrapper div");
    }
}
