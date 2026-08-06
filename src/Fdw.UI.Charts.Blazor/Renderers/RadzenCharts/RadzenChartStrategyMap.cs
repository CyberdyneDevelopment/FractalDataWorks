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
    // Why: static initializer for the strategy dictionary. Each entry is a named builder
    // function keyed on the ChartTypes registry name (Ordinal comparison at lookup time).
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

    // Why: resolve a field name from the Encodings list by role name (Ordinal comparison).
    private static string? FieldForRole(IChartModel model, string roleName)
        => model.Encodings.FirstOrDefault(
            e => string.Equals(e.Role.Name, roleName, StringComparison.Ordinal))?.FieldName;

    // Why: extract a decimal value from a row field; non-numeric values resolve to null so the
    // chart renders a gap rather than throwing. A gap is preferable to a crash.
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

    // Why: extract a string display value from a row field for category labels.
    private static string StringValue(IReadOnlyDictionary<string, object?> row, string? field)
    {
        if (field is null || !row.TryGetValue(field, out var raw))
            return string.Empty;
        return raw?.ToString() ?? string.Empty;
    }

    // Why: Radzen uses double internally for numeric values; convert once during projection so
    // Radzen's reflection-based ValueProperty binding never touches decimal.
    private static double? ToDouble(decimal? value)
        => value is { } d ? (double)d : (double?)null;

    // Why: project a single row to RadzenChartDataRow using the resolved field names so the
    // series fragment can bind CategoryProperty/ValueProperty to fixed property names.
    private static RadzenChartDataRow ProjectRow(
        IReadOnlyDictionary<string, object?> row, string? xField, string? yField)
        => new RadzenChartDataRow(StringValue(row, xField), ToDouble(NumericValue(row, yField)));

    // Why: build shared series groups — if a Series encoding is bound, partition rows by
    // distinct series values and emit one Radzen series per partition; otherwise emit one
    // series for the whole row set.  Returns (seriesName, projectedItems) pairs.
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

    // Why: each helper captures pre-projected groups in a closure and returns a RenderFragment
    // that uses RenderTreeBuilder to emit the correct Radzen series component(s).  The
    // per-type helper keeps the capture explicit, and the caller (strategy function) keeps
    // the builder call site clean.

    // Why: RenderTreeBuilder sequence numbers MUST be compile-time constants (MA0123). The
    // Blazor-endorsed pattern for loops is to reuse the SAME literal constants on every
    // iteration — the diffing engine keys loop items positionally, not by sequence number.
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
        // Why: donut uses Measure role first if bound (KPI pattern), falls back to Y.
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
