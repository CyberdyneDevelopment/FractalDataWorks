using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Fdw.UI.Abstractions.Charts;

namespace Fdw.UI.Charts.Blazor.Renderers.ECharts;

/// <summary>
/// Data-driven dispatch table that maps <see cref="ChartTypes"/> registry names to per-chart-type
/// ECharts option factory functions.
/// </summary>
/// <remarks>
/// <para>
/// Using a static dictionary instead of a switch/if-else chain satisfies FDW019 (no if-else
/// chains of 3+ arms on a TypeCollection name). New chart types are added by inserting a new
/// entry — no branching logic change required.
/// </para>
/// <para>
/// Each strategy receives the render-agnostic <see cref="IChartModel"/> (chart type + encodings
/// + display flags) and the caller-supplied rows. It returns a
/// <see cref="Dictionary{TKey,TValue}">Dictionary&lt;string, object?&gt;</see> representing the
/// ECharts <c>option</c> object, which is serialized to JSON and passed to the JS interop module.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static class EChartsStrategyMap
{
    private static readonly Dictionary<string, Func<IChartModel, IReadOnlyList<IReadOnlyDictionary<string, object?>>, Dictionary<string, object?>>> _strategies =
        new Dictionary<string, Func<IChartModel, IReadOnlyList<IReadOnlyDictionary<string, object?>>, Dictionary<string, object?>>>(StringComparer.Ordinal)
        {
            ["Bar"]     = BuildBarOption,
            ["Line"]    = BuildLineOption,
            ["Area"]    = BuildAreaOption,
            ["Pie"]     = BuildPieOption,
            ["Donut"]   = BuildDonutOption,
            ["Scatter"] = BuildScatterOption,
            ["Heatmap"] = BuildHeatmapOption,
            ["Sankey"]  = BuildSankeyOption,
        };

    /// <summary>
    /// Returns the strategy factory for the given chart type name, or <see langword="null"/> if
    /// no strategy is registered for that name.
    /// </summary>
    /// <param name="chartTypeName">The <see cref="ChartTypes"/> registry name (case-sensitive).</param>
    /// <returns>The strategy factory, or <see langword="null"/>.</returns>
    public static Func<IChartModel, IReadOnlyList<IReadOnlyDictionary<string, object?>>, Dictionary<string, object?>>? For(string chartTypeName)
    {
        _strategies.TryGetValue(chartTypeName, out var strategy);
        return strategy;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────────────

    private static string? FieldForRole(IChartModel model, string roleName)
        => model.Encodings.FirstOrDefault(
            e => string.Equals(e.Role.Name, roleName, StringComparison.Ordinal))?.FieldName;

    private static double? NumericValue(IReadOnlyDictionary<string, object?> row, string? field)
    {
        if (field is null || !row.TryGetValue(field, out var raw) || raw is null)
            return null;

        return raw switch
        {
            double db  => db,
            float f    => f,
            decimal d  => (double)d,
            int i      => i,
            long l     => l,
            string s   => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : null,
            _          => null,
        };
    }

    private static string StringValue(IReadOnlyDictionary<string, object?> row, string? field)
    {
        if (field is null || !row.TryGetValue(field, out var raw))
            return string.Empty;
        return raw?.ToString() ?? string.Empty;
    }

    private static Dictionary<string, object?> BaseOption(IChartModel model)
    {
        var opt = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (!string.IsNullOrEmpty(model.Title))
        {
            var title = new Dictionary<string, object?>(StringComparer.Ordinal) { ["text"] = model.Title };
            if (!string.IsNullOrEmpty(model.Subtitle))
                title["subtext"] = model.Subtitle;
            opt["title"] = title;
        }

        if (model.ShowLegend)
            opt["legend"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["show"] = true };

        if (model.EnableTooltips)
            opt["tooltip"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["trigger"] = "axis" };

        if (model.EnableZoom)
            opt["dataZoom"] = new List<object?>
            {
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "inside" },
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "slider" },
            };

        return opt;
    }

    private static Dictionary<string, object?> BuildXyOption(
        IChartModel model,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        string seriesType,
        bool fillArea = false)
    {
        var opt = BaseOption(model);
        var xField      = FieldForRole(model, "X");
        var yField      = FieldForRole(model, "Y");
        var seriesField = FieldForRole(model, "Series");

        var xCategories = rows
            .Select(r => StringValue(r, xField))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var xAxis = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = "category",
            ["data"] = xCategories,
        };
        if (model.ShowXAxisLabel && xField is not null)
            xAxis["name"] = xField;

        var yAxis = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "value" };
        if (model.ShowYAxisLabel && yField is not null)
            yAxis["name"] = yField;

        opt["xAxis"] = xAxis;
        opt["yAxis"] = yAxis;

        var seriesList = new List<object?>();

        if (seriesField is not null)
        {
            foreach (var group in rows.GroupBy(r => StringValue(r, seriesField), StringComparer.Ordinal))
            {
                var data = BuildCategoryData(group.ToList(), xCategories, xField, yField);
                var s = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["name"] = group.Key,
                    ["type"] = seriesType,
                    ["data"] = data,
                };
                if (fillArea)
                    s["areaStyle"] = new Dictionary<string, object?>(StringComparer.Ordinal);
                seriesList.Add(s);
            }
        }
        else
        {
            var data = BuildCategoryData(rows.ToList(), xCategories, xField, yField);
            var s = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"] = model.Title,
                ["type"] = seriesType,
                ["data"] = data,
            };
            if (fillArea)
                s["areaStyle"] = new Dictionary<string, object?>(StringComparer.Ordinal);
            seriesList.Add(s);
        }

        opt["series"] = seriesList;
        return opt;
    }

    private static List<object?> BuildCategoryData(
        List<IReadOnlyDictionary<string, object?>> rows,
        List<string> xCategories,
        string? xField,
        string? yField)
    {
        var data = new List<object?>(xCategories.Count);
        foreach (var x in xCategories)
        {
            var row = rows.FirstOrDefault(r =>
                string.Equals(StringValue(r, xField), x, StringComparison.Ordinal));
            data.Add(row is not null ? NumericValue(row, yField) : null);
        }
        return data;
    }

    // ── Per-type strategies ──────────────────────────────────────────────────────────────────

    private static Dictionary<string, object?> BuildBarOption(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        => BuildXyOption(model, rows, "bar");

    private static Dictionary<string, object?> BuildLineOption(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        => BuildXyOption(model, rows, "line");

    private static Dictionary<string, object?> BuildAreaOption(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        => BuildXyOption(model, rows, "line", fillArea: true);

    private static Dictionary<string, object?> BuildPieOption(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var opt = BaseOption(model);

        if (model.EnableTooltips)
            opt["tooltip"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["trigger"] = "item" };

        var xField = FieldForRole(model, "X");
        var yField = FieldForRole(model, "Y");

        var data = rows
            .Select(r => (object?)new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"]  = StringValue(r, xField),
                ["value"] = NumericValue(r, yField),
            })
            .ToList();

        opt["series"] = new List<object?>
        {
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"] = model.Title,
                ["type"] = "pie",
                ["data"] = data,
            },
        };

        return opt;
    }

    private static Dictionary<string, object?> BuildDonutOption(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var opt = BaseOption(model);

        if (model.EnableTooltips)
            opt["tooltip"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["trigger"] = "item" };

        var xField = FieldForRole(model, "X");
        var yField = FieldForRole(model, "Measure") ?? FieldForRole(model, "Y");

        var data = rows
            .Select(r => (object?)new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"]  = StringValue(r, xField),
                ["value"] = NumericValue(r, yField),
            })
            .ToList();

        opt["series"] = new List<object?>
        {
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"]   = model.Title,
                ["type"]   = "pie",
                ["radius"] = new List<object?> { "40%", "70%" },
                ["data"]   = data,
            },
        };

        return opt;
    }

    private static Dictionary<string, object?> BuildScatterOption(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var opt = BaseOption(model);

        var xField      = FieldForRole(model, "X");
        var yField      = FieldForRole(model, "Y");
        var seriesField = FieldForRole(model, "Series");

        var xAxis = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "value" };
        if (model.ShowXAxisLabel && xField is not null)
            xAxis["name"] = xField;
        var yAxis = new Dictionary<string, object?>(StringComparer.Ordinal) { ["type"] = "value" };
        if (model.ShowYAxisLabel && yField is not null)
            yAxis["name"] = yField;

        opt["xAxis"] = xAxis;
        opt["yAxis"] = yAxis;

        var seriesList = new List<object?>();

        if (seriesField is not null)
        {
            foreach (var group in rows.GroupBy(r => StringValue(r, seriesField), StringComparer.Ordinal))
            {
                var points = group
                    .Select(r => (object?)new List<object?> { NumericValue(r, xField), NumericValue(r, yField) })
                    .ToList();
                seriesList.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["name"] = group.Key,
                    ["type"] = "scatter",
                    ["data"] = points,
                });
            }
        }
        else
        {
            var points = rows
                .Select(r => (object?)new List<object?> { NumericValue(r, xField), NumericValue(r, yField) })
                .ToList();
            seriesList.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"] = model.Title,
                ["type"] = "scatter",
                ["data"] = points,
            });
        }

        opt["series"] = seriesList;
        return opt;
    }

    private static Dictionary<string, object?> BuildHeatmapOption(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var opt = BaseOption(model);

        if (model.EnableTooltips)
            opt["tooltip"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["trigger"]   = "item",
                ["formatter"] = "{a} <br/>{b}: {c}",
            };

        var xField      = FieldForRole(model, "X");
        var seriesField = FieldForRole(model, "Series");
        var yField      = FieldForRole(model, "Y");

        var xCategories = rows
            .Select(r => StringValue(r, xField))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var yCategories = seriesField is not null
            ? rows.Select(r => StringValue(r, seriesField)).Distinct(StringComparer.Ordinal).ToList()
            : new List<string> { model.Title };

        var values = rows
            .Select(r => NumericValue(r, yField))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        var minVal = values.Count > 0 ? values.Min() : 0.0;
        var maxVal = values.Count > 0 ? values.Max() : 1.0;
        if (Math.Abs(maxVal - minVal) < double.Epsilon)
            maxVal = minVal + 1.0;

        var data = new List<object?>();
        foreach (var row in rows)
        {
            var xVal   = StringValue(row, xField);
            var yVal   = seriesField is not null ? StringValue(row, seriesField) : model.Title;
            var xIdx   = xCategories.IndexOf(xVal);
            var yIdx   = yCategories.IndexOf(yVal);
            var value  = NumericValue(row, yField);
            if (xIdx >= 0 && yIdx >= 0)
                data.Add(new List<object?> { xIdx, yIdx, value });
        }

        opt["xAxis"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = "category",
            ["data"] = xCategories,
        };
        opt["yAxis"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = "category",
            ["data"] = yCategories,
        };
        opt["visualMap"] = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["min"]        = minVal,
            ["max"]        = maxVal,
            ["calculable"] = true,
        };
        opt["series"] = new List<object?>
        {
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"] = model.Title,
                ["type"] = "heatmap",
                ["data"] = data,
                ["label"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["show"] = false },
            },
        };

        return opt;
    }

    private static Dictionary<string, object?> BuildSankeyOption(
        IChartModel model, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var opt = BaseOption(model);

        if (model.EnableTooltips)
            opt["tooltip"] = new Dictionary<string, object?>(StringComparer.Ordinal) { ["trigger"] = "item" };

        var sourceField = FieldForRole(model, "Source");
        var targetField = FieldForRole(model, "Target");
        var weightField = FieldForRole(model, "Weight");

        var nodeNames = rows
            .SelectMany(r => new[]
            {
                StringValue(r, sourceField),
                StringValue(r, targetField),
            })
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var nodes = nodeNames
            .Select(n => (object?)new Dictionary<string, object?>(StringComparer.Ordinal) { ["name"] = n })
            .ToList();

        var links = rows
            .Where(r =>
                !string.IsNullOrEmpty(StringValue(r, sourceField)) &&
                !string.IsNullOrEmpty(StringValue(r, targetField)))
            .Select(r => (object?)new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["source"] = StringValue(r, sourceField),
                ["target"] = StringValue(r, targetField),
                ["value"]  = NumericValue(r, weightField) ?? 1.0,
            })
            .ToList();

        opt["series"] = new List<object?>
        {
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"]  = model.Title,
                ["type"]  = "sankey",
                ["data"]  = nodes,
                ["links"] = links,
            },
        };

        return opt;
    }
}
