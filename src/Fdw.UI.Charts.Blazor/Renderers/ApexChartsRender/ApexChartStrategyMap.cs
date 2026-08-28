using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using ApexCharts;
using Fdw.UI.Abstractions.Charts;

namespace Fdw.UI.Charts.Blazor.Renderers.ApexChartsRender;

/// <summary>
/// Data-driven dispatch table that maps <see cref="ChartTypes"/> registry names to per-chart-type
/// <see cref="ApexChartConfiguration"/> factory functions.
/// </summary>
/// <remarks>
/// <para>
/// Using a static dictionary instead of a switch/if-else chain satisfies FDW019 (no if-else
/// chains of 3+ arms on a TypeCollection name). New chart types are added by inserting a new
/// entry — no branching logic change required.
/// </para>
/// <para>
/// Each strategy receives the render-agnostic <see cref="IChartModel"/> (chart type + encodings
/// + display flags) and the caller-supplied rows. It returns an <see cref="ApexChartConfiguration"/>
/// that the <c>ApexChartsRenderer</c> razor markup consumes directly.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static class ApexChartStrategyMap
{
    private static readonly Dictionary<string, Func<IChartModel, IReadOnlyList<IReadOnlyDictionary<string, object?>>, ApexChartConfiguration>> _strategies =
        new Dictionary<string, Func<IChartModel, IReadOnlyList<IReadOnlyDictionary<string, object?>>, ApexChartConfiguration>>(StringComparer.Ordinal)
        {
            ["Bar"]     = BuildBarConfiguration,
            ["Line"]    = BuildLineConfiguration,
            ["Area"]    = BuildAreaConfiguration,
            ["Pie"]     = BuildPieConfiguration,
            ["Donut"]   = BuildDonutConfiguration,
            ["Scatter"] = BuildScatterConfiguration,
            ["Heatmap"] = BuildHeatmapConfiguration,
            ["Kpi"]     = BuildKpiConfiguration,
        };

    /// <summary>
    /// Returns the strategy factory for the given chart type name, or <see langword="null"/> if
    /// no strategy is registered for that name.
    /// </summary>
    /// <param name="chartTypeName">The <see cref="ChartTypes"/> registry name (case-sensitive).</param>
    /// <returns>The strategy factory, or <see langword="null"/>.</returns>
    public static Func<IChartModel, IReadOnlyList<IReadOnlyDictionary<string, object?>>, ApexChartConfiguration>? For(string chartTypeName)
    {
        _strategies.TryGetValue(chartTypeName, out var strategy);
        return strategy;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────────────────

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

    private static ApexChartOptions<ChartDataRow> BaseOptions(IChartModel model)
    {
        var options = new ApexChartOptions<ChartDataRow>
        {
            Chart = new Chart { Toolbar = new Toolbar { Show = model.EnableZoom } },
            Legend = new Legend { Show = model.ShowLegend },
            Tooltip = new Tooltip { Enabled = model.EnableTooltips },
        };

        if (!string.IsNullOrEmpty(model.Title))
        {
            options.Title = new Title { Text = model.Title, Align = Align.Left };
            if (!string.IsNullOrEmpty(model.Subtitle))
                options.Subtitle = new Subtitle { Text = model.Subtitle };
        }

        return options;
    }

    private static List<ApexChartSeries> BuildXySeries(
        IChartModel model,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        SeriesType seriesType)
    {
        var xField = FieldForRole(model, "X");
        var yField = FieldForRole(model, "Y");
        var seriesField = FieldForRole(model, "Series");

        if (seriesField is not null)
        {
            return rows
                .GroupBy(r => StringValue(r, seriesField), StringComparer.Ordinal)
                .Select(g => new ApexChartSeries
                {
                    Name         = g.Key,
                    SeriesType   = seriesType,
                    Items        = g.Select(r => new ChartDataRow(r)).ToList(),
                    XValueSelector = r => StringValue(r.Fields, xField),
                    YValueSelector = r => NumericValue(r.Fields, yField),
                })
                .ToList();
        }

        return
        [
            new ApexChartSeries
            {
                Name         = model.Title,
                SeriesType   = seriesType,
                Items        = rows.Select(r => new ChartDataRow(r)).ToList(),
                XValueSelector = r => StringValue(r.Fields, xField),
                YValueSelector = r => NumericValue(r.Fields, yField),
            },
        ];
    }

    // ── Per-type strategies ───────────────────────────────────────────────────────────────────

    private static ApexChartConfiguration BuildBarConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var options = BaseOptions(model);
        options.Chart!.Type = ChartType.Bar;
        if (model.ShowXAxisLabel)
            options.Xaxis = new XAxis { Title = new AxisTitle { Text = FieldForRole(model, "X") ?? string.Empty } };
        if (model.ShowYAxisLabel)
            options.Yaxis = [new YAxis { Title = new AxisTitle { Text = FieldForRole(model, "Y") ?? string.Empty } }];

        return new ApexChartConfiguration
        {
            Options     = options,
            SeriesItems = BuildXySeries(model, rows, SeriesType.Bar),
        };
    }

    private static ApexChartConfiguration BuildLineConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var options = BaseOptions(model);
        options.Chart!.Type = ChartType.Line;
        if (model.ShowXAxisLabel)
            options.Xaxis = new XAxis { Title = new AxisTitle { Text = FieldForRole(model, "X") ?? string.Empty } };
        if (model.ShowYAxisLabel)
            options.Yaxis = [new YAxis { Title = new AxisTitle { Text = FieldForRole(model, "Y") ?? string.Empty } }];

        return new ApexChartConfiguration
        {
            Options     = options,
            SeriesItems = BuildXySeries(model, rows, SeriesType.Line),
        };
    }

    private static ApexChartConfiguration BuildAreaConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var options = BaseOptions(model);
        options.Chart!.Type = ChartType.Area;

        return new ApexChartConfiguration
        {
            Options     = options,
            SeriesItems = BuildXySeries(model, rows, SeriesType.Area),
        };
    }

    private static ApexChartConfiguration BuildPieConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var options = BaseOptions(model);
        options.Chart!.Type = ChartType.Pie;
        options.PlotOptions = new PlotOptions { Pie = new PlotOptionsPie { ExpandOnClick = true } };

        var xField = FieldForRole(model, "X");
        var yField = FieldForRole(model, "Y");

        var items = rows.Select(r => new ChartDataRow(r)).ToList();
        return new ApexChartConfiguration
        {
            Options = options,
            SeriesItems =
            [
                new ApexChartSeries
                {
                    Name           = model.Title,
                    SeriesType     = SeriesType.Donut,
                    Items          = items,
                    XValueSelector = r => StringValue(r.Fields, xField),
                    YValueSelector = r => NumericValue(r.Fields, yField),
                },
            ],
        };
    }

    private static ApexChartConfiguration BuildDonutConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var options = BaseOptions(model);
        options.Chart!.Type = ChartType.Donut;

        var xField = FieldForRole(model, "X");
        var yField = FieldForRole(model, "Y");

        var measureField = FieldForRole(model, "Measure") ?? yField;

        var items = rows.Select(r => new ChartDataRow(r)).ToList();
        return new ApexChartConfiguration
        {
            Options = options,
            SeriesItems =
            [
                new ApexChartSeries
                {
                    Name           = model.Title,
                    SeriesType     = SeriesType.Donut,
                    Items          = items,
                    XValueSelector = r => StringValue(r.Fields, xField),
                    YValueSelector = r => NumericValue(r.Fields, measureField),
                },
            ],
        };
    }

    private static ApexChartConfiguration BuildScatterConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var options = BaseOptions(model);
        options.Chart!.Type = ChartType.Scatter;

        return new ApexChartConfiguration
        {
            Options     = options,
            SeriesItems = BuildXySeries(model, rows, SeriesType.Scatter),
        };
    }

    private static ApexChartConfiguration BuildHeatmapConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var options = BaseOptions(model);
        options.Chart!.Type = ChartType.Heatmap;

        return new ApexChartConfiguration
        {
            Options     = options,
            SeriesItems = BuildXySeries(model, rows, SeriesType.Heatmap),
        };
    }

    private static ApexChartConfiguration BuildKpiConfiguration(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var options = BaseOptions(model);
        options.Chart!.Type = ChartType.RadialBar;

        var measureField = FieldForRole(model, "Measure") ?? FieldForRole(model, "Y");
        var firstRow = rows.Count > 0 ? rows[0] : null;

        var value = firstRow is not null ? NumericValue(firstRow, measureField) ?? 0m : 0m;

        return new ApexChartConfiguration
        {
            Options = options,
            SeriesItems =
            [
                new ApexChartSeries
                {
                    Name           = model.Title,
                    SeriesType     = SeriesType.RadialBar,
                    Items          = firstRow is not null ? [new ChartDataRow(firstRow)] : [],
                    XValueSelector = _ => model.Title,
                    YValueSelector = _ => value,
                },
            ],
        };
    }
}
