using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Schema;

namespace Fdw.Services.Workspaces.Charts;

/// <summary>
/// Checks that a saved chart can be drawn: its chart type exists, its bindings fit one way of binding that
/// type, and every field they name is a field of the data set that sits on an axis the slot accepts, or takes the aggregate the binding names (FDW-1023).
/// </summary>
/// <remarks>
/// The answer is the first thing that is wrong, in words that name the slot or the field, or null when
/// nothing is. It does not check <c>options</c> (the chart's own drawing reads those), and it does not say
/// whether a bound field can be grouped or summed by the data route: a calculated field fits a slot and is
/// refused when the chart is queried, by the route, which names it.
/// </remarks>
public static class ChartSpecificationCheck
{
    private static readonly string[] Aggregates =
        [DatumAggregates.Count, DatumAggregates.Sum, DatumAggregates.Avg, DatumAggregates.Min, DatumAggregates.Max];

    /// <summary>Checks a chart.</summary>
    /// <param name="chartType">The chart type's name.</param>
    /// <param name="encoding">The encoding as stored.</param>
    /// <param name="dataSetFields">The fields of the data set the chart reads.</param>
    /// <returns>The first thing that is wrong, or null when the chart fits.</returns>
    public static string? Reason(string chartType, string encoding, IEnumerable<DataSetFieldConfiguration> dataSetFields)
    {
        var type = ChartTypes.ByName(chartType);
        if (ReferenceEquals(type, ChartTypes.NotFound))
            return $"chart type '{chartType}' is not one of {string.Join(", ", ChartTypes.All().Where(t => !ReferenceEquals(t, ChartTypes.NotFound)).Select(t => t.Name))}";

        var read = ChartEncodingReader.Read(encoding);
        if (read.Reason is not null) return read.Reason;

        var fit = Fit(type, read.Encoding!.Bindings);
        return fit.Reason is not null
            ? fit.Reason
            : FieldsReason(fit.Shape!, read.Encoding.Bindings, dataSetFields.Where(f => f.IsCurrent && !f.IsDeleted).ToList());
    }

    /// <summary>The way of binding the chart's bindings fit, or why none does.</summary>
    private static (ChartBindingShape? Shape, string? Reason) Fit(IChartType type, IReadOnlyDictionary<string, IReadOnlyList<ChartBinding>> bindings)
    {
        var problems = type.Shapes.Select(shape => (Shape: shape, Problems: Problems(shape, bindings))).ToList();
        var fitting = problems.FirstOrDefault(p => p.Problems.Count == 0);
        if (fitting.Shape is not null) return (fitting.Shape, null);

        var closest = problems.OrderBy(p => p.Problems.Count).First();
        var alternatives = type.Shapes.Count > 1
            ? $"; a {type.Name} binds {string.Join(", or ", type.Shapes.Select(Describe))}"
            : string.Empty;
        return (null, $"{type.Name} chart: {closest.Problems[0]}{alternatives}");
    }

    private static List<string> Problems(ChartBindingShape shape, IReadOnlyDictionary<string, IReadOnlyList<ChartBinding>> bindings)
    {
        var problems = bindings.Keys
            .Where(name => !shape.Slots.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Select(name => $"it has no slot '{name}'")
            .ToList();
        problems.AddRange(shape.Slots
            .Where(s => s.IsRequired && !bindings.ContainsKey(s.Name))
            .Select(s => $"slot '{s.Name}' needs a field"));
        return problems;
    }

    private static string Describe(ChartBindingShape shape)
        => string.Join(" + ", shape.Slots.Select(s => s.IsRequired ? s.Name : $"{s.Name} (optional)"));

    private static string? FieldsReason(
        ChartBindingShape shape, IReadOnlyDictionary<string, IReadOnlyList<ChartBinding>> bindings, List<DataSetFieldConfiguration> fields)
    {
        foreach (var slot in shape.Slots.Where(s => bindings.ContainsKey(s.Name)))
        {
            var reason = SlotReason(slot, bindings[slot.Name], fields);
            if (reason is not null) return reason;
        }

        return null;
    }

    private static string? SlotReason(ChartSlot slot, IReadOnlyList<ChartBinding> bound, List<DataSetFieldConfiguration> fields)
    {
        if (!slot.AllowsMany && bound.Count > 1)
            return $"slot '{slot.Name}' takes one field, not {bound.Count}";

        foreach (var binding in bound)
        {
            var reason = BindingReason(slot, binding, fields);
            if (reason is not null) return reason;
        }

        return null;
    }

    private static string? BindingReason(ChartSlot slot, ChartBinding binding, List<DataSetFieldConfiguration> fields)
    {
        if (binding.Field is null) return AggregateReason(slot, binding, null);

        var field = fields.FirstOrDefault(f => string.Equals(f.Name, binding.Field, StringComparison.OrdinalIgnoreCase));
        if (field is null) return $"slot '{slot.Name}' names field '{binding.Field}', which the data set does not have";

        return binding.Aggregate is null ? AxisReason(slot, field) : AggregateReason(slot, binding, field);
    }

    private static string? AxisReason(ChartSlot slot, DataSetFieldConfiguration field)
    {
        var datum = DatumTypes.ById(field.DatumTypeId);
        if (slot.Axes.Any(datum.AcceptsAxis)) return null;

        var asValue = slot.Axes.Contains(AxisTypes.Measure.Name, StringComparer.OrdinalIgnoreCase)
            ? $"; as a value it needs an aggregate, which {datum.Name} allows as {string.Join(", ", datum.Aggregates)}"
            : string.Empty;
        return $"slot '{slot.Name}' needs a {string.Join(" or ", slot.Axes)} field, but '{field.Name}' is typed {datum.Name}{asValue}";
    }

    /// <summary>The reason an aggregate cannot be plotted in a slot, or null when it can.</summary>
    /// <remarks>
    /// An aggregate yields a number, so only a slot that takes a Measure takes one. The datum type says which
    /// aggregates its fields allow; Min and Max of something that is not a number do not yield one either.
    /// </remarks>
    private static string? AggregateReason(ChartSlot slot, ChartBinding binding, DataSetFieldConfiguration? field)
    {
        if (!slot.Axes.Contains(AxisTypes.Measure.Name, StringComparer.OrdinalIgnoreCase))
            return $"slot '{slot.Name}' takes a field as it stands, not an aggregate";

        var aggregate = Aggregates.FirstOrDefault(a => string.Equals(a, binding.Aggregate, StringComparison.OrdinalIgnoreCase));
        if (aggregate is null)
            return $"slot '{slot.Name}': aggregate '{binding.Aggregate}' is not one of {string.Join(", ", Aggregates)}";

        if (field is null)
            return string.Equals(aggregate, DatumAggregates.Count, StringComparison.Ordinal) ? null : $"slot '{slot.Name}': {aggregate} needs a field; only Count may name none";

        var datum = DatumTypes.ById(field.DatumTypeId);
        if (!datum.AllowsAggregate(aggregate))
            return $"slot '{slot.Name}' cannot plot {aggregate} of '{field.Name}': it is typed {datum.Name}, which allows {string.Join(", ", datum.Aggregates)}";

        return aggregate is DatumAggregates.Min or DatumAggregates.Max && !datum.AcceptsAxis(AxisTypes.Measure.Name)
            ? $"slot '{slot.Name}' cannot plot {aggregate} of '{field.Name}': it is typed {datum.Name}, so the result is not a number"
            : null;
    }
}
