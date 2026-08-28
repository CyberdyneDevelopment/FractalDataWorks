using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Fdw.UI.Abstractions.Charts;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Fdw.UI.Charts.Blazor.Renderers.RadzenCharts;

/// <summary>
/// Data-driven dispatch table that maps <see cref="ChartTypes"/> registry names to
/// per-chart-type <see cref="RadzenChartConfiguration"/> factory functions.
/// </summary>
/// <remarks>
/// <para>
/// Using a static dictionary instead of a switch/if-else chain satisfies FDW019 (no if-else
/// chains of 3+ arms on a TypeCollection name). New chart types are added by inserting a new
/// entry — no branching logic change required.
/// </para>
/// <para>
/// Each strategy pre-projects the raw row dictionaries into
/// <see cref="RadzenChartDataRow"/> lists and builds a <see cref="RenderFragment"/> that
/// emits the correct Radzen series component(s) (e.g. <c>RadzenColumnSeries</c>) using the
/// Blazor <c>RenderTreeBuilder</c> API. The fragment is evaluated at render time inside
/// <c>RadzenChartsRenderer.razor</c>, where it is a child of <c>RadzenChart</c> and
/// therefore receives the chart's cascading parameters correctly.
/// </para>
/// <para>
/// Supported chart types: Bar, Line, Area, Pie, Donut, Scatter.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static class RadzenChartStrategyMap
{
    private static readonly Dictionary<string, Func<IChartModel, IReadOnlyList<IReadOnlyDictionary<string, object?>>, RadzenChartConfiguration>> _strategies =
        new Dictionary<string, Func<IChartModel, IReadOnlyList<IReadOnlyDictionary<string, object?>>, RadzenChartConfiguration>>(StringComparer.Ordinal)
        {
            ["Bar"]     = BuildBarConfiguration,
            ["Line"]    = BuildLineConfiguration,
            ["Area"]    = BuildAreaConfiguration,
            ["Pie"]     = BuildPieConfiguration,
            ["Donut"]   = BuildDonutConfiguration,
            ["Scatter"] = BuildScatterConfiguration,
        };

    /// <summary>
    /// Returns the strategy factory for the given chart type name, or <see langword="null"/>
    /// if no strategy is registered for that name.
    /// </summary>
    /// <param name="chartTypeName">The <see cref="ChartTypes"/> registry name (case-sensitive).</param>
    /// <returns>The strategy factory, or <see langword="null"/>.</returns>
    public static Func<IChartModel, IReadOnlyList<IReadOnlyDictionary<string, object?>>, RadzenChartConfiguration>? For(string chartTypeName)
    {
        _strategies.TryGetValue(chartTypeName, out var strategy);
        return strategy;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────

    private static string? FieldForRole(IChartModel model, string roleName)
        => model.Encodings.FirstOrDefault(
            e => string.Equals(e.Role.Name, roleName, StringComparison.Ordinal))?.FieldName;

    private static decimal? NumericValue(IReadOnlyDictionary<string, object?> row, string? field)
    {
        if (field is null || !row.TryGetValue(field, out var raw) || raw is null)
            return null;

        return raw switch
        {
            decimal d  => d,
            double db  => (decimal)db,
            float f    => (decimal)f,
            int i      => i,
            long l     => l,
            string s   => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : null,
            _          => null,
        };
    }

    private static string StringValue(IReadOnlyDictionary<string, object?> row, string? field)
    {
        if (field is null || !row.TryGetValue(field, out var raw))
            return string.Empty;
        return raw?.ToString() ?? string.Empty;
    }

    private static double? ToDouble(decimal? value)
        => value is { } d ? (double)d : (double?)null;

    private static RadzenChartDataRow ProjectRow(
        IReadOnlyDictionary<string, object?> row, string? xField, string? yField)
        => new RadzenChartDataRow(StringValue(row, xField), ToDouble(NumericValue(row, yField)));

    private static List<(string Name, List<RadzenChartDataRow> Items)> BuildSeriesGroups(
        IChartModel model,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        string? xField,
        string? yField)
    {
        var seriesField = FieldForRole(model, "Series");

        if (seriesField is not null)
        {
            return rows
                .GroupBy(r => StringValue(r, seriesField), StringComparer.Ordinal)
                .Select(g =>
                    (g.Key, g.Select(r => ProjectRow(r, xField, yField)).ToList()))
                .ToList();
        }

        return [(model.Title, rows.Select(r => ProjectRow(r, xField, yField)).ToList())];
    }

    // ── RenderFragment builders ──────────────────────────────────────────────────────


    private static RenderFragment BuildColumnSeriesFragment(
        List<(string Name, List<RadzenChartDataRow> Items)> groups)
        => builder =>
        {
            foreach (var (name, items) in groups)
            {
                builder.OpenComponent<RadzenColumnSeries<RadzenChartDataRow>>(0);
                builder.AddAttribute(1, "Data", items);
                builder.AddAttribute(2, "CategoryProperty", "Category");
                builder.AddAttribute(3, "ValueProperty", "Value");
                builder.AddAttribute(4, "Title", name);
                builder.CloseComponent();
            }
        };

    private static RenderFragment BuildLineSeriesFragment(
        List<(string Name, List<RadzenChartDataRow> Items)> groups)
        => builder =>
        {
            foreach (var (name, items) in groups)
            {
                builder.OpenComponent<RadzenLineSeries<RadzenChartDataRow>>(0);
                builder.AddAttribute(1, "Data", items);
                builder.AddAttribute(2, "CategoryProperty", "Category");
                builder.AddAttribute(3, "ValueProperty", "Value");
                builder.AddAttribute(4, "Title", name);
                builder.CloseComponent();
            }
        };

    private static RenderFragment BuildAreaSeriesFragment(
        List<(string Name, List<RadzenChartDataRow> Items)> groups)
        => builder =>
        {
            foreach (var (name, items) in groups)
            {
                builder.OpenComponent<RadzenAreaSeries<RadzenChartDataRow>>(0);
                builder.AddAttribute(1, "Data", items);
                builder.AddAttribute(2, "CategoryProperty", "Category");
                builder.AddAttribute(3, "ValueProperty", "Value");
                builder.AddAttribute(4, "Title", name);
                builder.CloseComponent();
            }
        };

    private static RenderFragment BuildScatterSeriesFragment(
        List<(string Name, List<RadzenChartDataRow> Items)> groups)
        => builder =>
        {
            foreach (var (name, items) in groups)
            {
                builder.OpenComponent<RadzenScatterSeries<RadzenChartDataRow>>(0);
                builder.AddAttribute(1, "Data", items);
                builder.AddAttribute(2, "CategoryProperty", "Category");
                builder.AddAttribute(3, "ValueProperty", "Value");
                builder.AddAttribute(4, "Title", name);
                builder.CloseComponent();
            }
        };

    private static RenderFragment BuildPieSeriesFragment(
        string title, List<RadzenChartDataRow> items)
        => builder =>
        {
            builder.OpenComponent<RadzenPieSeries<RadzenChartDataRow>>(0);
            builder.AddAttribute(1, "Data", items);
            builder.AddAttribute(2, "CategoryProperty", "Category");
            builder.AddAttribute(3, "ValueProperty", "Value");
            builder.AddAttribute(4, "Title", title);
            builder.CloseComponent();
        };

    private static RenderFragment BuildDonutSeriesFragment(
        string title, List<RadzenChartDataRow> items)
        => builder =>
        {
            builder.OpenComponent<RadzenDonutSeries<RadzenChartDataRow>>(0);
            builder.AddAttribute(1, "Data", items);
            builder.AddAttribute(2, "CategoryProperty", "Category");
            builder.AddAttribute(3, "ValueProperty", "Value");
            builder.AddAttribute(4, "Title", title);
            builder.CloseComponent();
        };

    // ── Per-type strategies ──────────────────────────────────────────────────────────

    private static RadzenChartConfiguration BuildBarConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var groups = BuildSeriesGroups(model, rows, FieldForRole(model, "X"), FieldForRole(model, "Y"));
        return new RadzenChartConfiguration
        {
            Title          = model.Title,
            ShowLegend     = model.ShowLegend,
            EnableTooltips = model.EnableTooltips,
            SeriesFragment = BuildColumnSeriesFragment(groups),
        };
    }

    private static RadzenChartConfiguration BuildLineConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var groups = BuildSeriesGroups(model, rows, FieldForRole(model, "X"), FieldForRole(model, "Y"));
        return new RadzenChartConfiguration
        {
            Title          = model.Title,
            ShowLegend     = model.ShowLegend,
            EnableTooltips = model.EnableTooltips,
            SeriesFragment = BuildLineSeriesFragment(groups),
        };
    }

    private static RadzenChartConfiguration BuildAreaConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var groups = BuildSeriesGroups(model, rows, FieldForRole(model, "X"), FieldForRole(model, "Y"));
        return new RadzenChartConfiguration
        {
            Title          = model.Title,
            ShowLegend     = model.ShowLegend,
            EnableTooltips = model.EnableTooltips,
            SeriesFragment = BuildAreaSeriesFragment(groups),
        };
    }

    private static RadzenChartConfiguration BuildPieConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var xField = FieldForRole(model, "X");
        var yField = FieldForRole(model, "Y");
        var items  = rows.Select(r => ProjectRow(r, xField, yField)).ToList();
        return new RadzenChartConfiguration
        {
            Title          = model.Title,
            ShowLegend     = model.ShowLegend,
            EnableTooltips = model.EnableTooltips,
            SeriesFragment = BuildPieSeriesFragment(model.Title, items),
        };
    }

    private static RadzenChartConfiguration BuildDonutConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var xField = FieldForRole(model, "X");
        var yField = FieldForRole(model, "Measure") ?? FieldForRole(model, "Y");
        var items  = rows.Select(r => ProjectRow(r, xField, yField)).ToList();
        return new RadzenChartConfiguration
        {
            Title          = model.Title,
            ShowLegend     = model.ShowLegend,
            EnableTooltips = model.EnableTooltips,
            SeriesFragment = BuildDonutSeriesFragment(model.Title, items),
        };
    }

    private static RadzenChartConfiguration BuildScatterConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var groups = BuildSeriesGroups(model, rows, FieldForRole(model, "X"), FieldForRole(model, "Y"));
        return new RadzenChartConfiguration
        {
            Title          = model.Title,
            ShowLegend     = model.ShowLegend,
            EnableTooltips = model.EnableTooltips,
            SeriesFragment = BuildScatterSeriesFragment(groups),
        };
    }
}
