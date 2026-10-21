using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// What a component sees at each input once the mappings on its inputs are applied (FDW-2013), and the design-time check of those mappings.
/// </summary>
/// <remarks>
/// A node's own fields stay the fields that arrive, because they are what the person building the mapping picks from; the component's fields are
/// worked out from the mapped ones. A mapping from a field that does not arrive is a problem of the design, listed in <see cref="DescribeContext.Problems"/>
/// when the fields that arrive are known; what only the run can say (a value that will not convert) fails the run.
/// </remarks>
internal static class InputMappings
{
    /// <summary>Applies the mappings on a component's inputs to the fields that arrive.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="received">The fields that arrive at each input node, <see langword="null"/> where they cannot be known.</param>
    /// <param name="context">Receives the problems found.</param>
    /// <returns>The fields the component sees at each input: the mapped fields for a mapped input, the fields that arrive otherwise.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<GraphField>?> Apply(
        PipelineComponentConfiguration component, IReadOnlyDictionary<string, IReadOnlyList<GraphField>?> received, DescribeContext context)
    {
        var rows = component.Fields
            .Where(field => string.Equals(field.SettingName, ComponentTypeBase.MappingsSetting, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var seen = new Dictionary<string, IReadOnlyList<GraphField>?>(received, StringComparer.OrdinalIgnoreCase);

        // Why unknown: a list a run supplies is chosen when the run starts, so the fields it gives cannot be known now (FDW-1745).
        if (ParameterSettings.IsBound(component, ComponentTypeBase.MappingsSetting))
        {
            foreach (var input in received.Keys)
            {
                seen[input] = null;
            }

            return seen;
        }

        foreach (var input in rows.Select(row => row.InputName).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!received.TryGetValue(input, out var incoming))
            {
                continue;
            }

            var mine = rows.Where(row => string.Equals(row.InputName, input, StringComparison.OrdinalIgnoreCase)).OrderBy(row => row.Ordinal).ToList();
            Check(component, input, mine, incoming, context);
            seen[input] = Fields(mine, incoming);
        }

        return seen;
    }

    /// <summary>Lists the fields mappings produce, each typed by its type to convert to, its constant or the field it takes.</summary>
    /// <param name="rows">The mappings, in order.</param>
    /// <param name="source">The fields the mappings read from, or <see langword="null"/> when they cannot be known.</param>
    /// <returns>The fields, in order.</returns>
    public static IReadOnlyList<GraphField> Fields(IEnumerable<PipelineComponentFieldConfiguration> rows, IReadOnlyList<GraphField>? source) =>
        [.. rows.Select(row => new GraphField(row.TargetField, TypeOf(row, source), OriginOf(row, source)))];

    // A mapping from a field of the input has the origin of that field, whatever it is renamed to or converted to (FDW-2156); a constant has none.
    private static GraphFieldOrigin? OriginOf(PipelineComponentFieldConfiguration row, IReadOnlyList<GraphField>? source) =>
        row.SourceField is { } name
            ? source?.FirstOrDefault(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))?.Origin
            : null;

    // A mapping that states a type to convert to has that type; otherwise it has its source field's type, or its constant's.
    private static string? TypeOf(PipelineComponentFieldConfiguration row, IReadOnlyList<GraphField>? source)
    {
        if (row.TargetType is { } converted)
        {
            return converted;
        }

        return row.SourceField is { } name
            ? source?.FirstOrDefault(field => string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))?.Type
            : row.ConstantType;
    }

    // A source field that does not arrive, with no default to take its place, is a problem of the design while the fields that arrive are known.
    private static void Check(
        PipelineComponentConfiguration component, string input, List<PipelineComponentFieldConfiguration> rows, IReadOnlyList<GraphField>? arriving, DescribeContext context)
    {
        if (arriving is not { Count: > 0 })
        {
            return;
        }

        foreach (var row in rows.Where(row => row.SourceField is not null && row.DefaultText is null))
        {
            if (!arriving.Any(field => string.Equals(field.Name, row.SourceField, StringComparison.OrdinalIgnoreCase)))
            {
                context.Problems.Add(new GraphProblem(
                    ["components", component.Name, "settings", ComponentTypeBase.MappingsSetting],
                    $"component '{component.Name}' maps '{row.TargetField}' from '{row.SourceField}' on its input '{input}', but no field '{row.SourceField}' arrives there"));
            }
        }
    }
}
