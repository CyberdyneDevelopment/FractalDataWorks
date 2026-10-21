using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Reads the field maps a run supplied for a component's input (FDW-1745): each item is a source field, an arrow and a target field,
/// <c>Name -&gt; BreweryName</c>.
/// </summary>
/// <remarks>
/// An item states both fields. No target is assumed for an item that states only a source, and an item with two arrows is not read as
/// one, because a field name cannot hold the arrow.
/// </remarks>
internal static class FieldMapPairs
{
    private const string Arrow = "->";

    /// <summary>Turns the items a run supplied into the rows of the component's field maps.</summary>
    /// <param name="setting">The name of the setting the maps are for.</param>
    /// <param name="input">The input node the maps are on.</param>
    /// <param name="items">The items the run supplied, in order.</param>
    /// <returns>One row per item, or <see langword="null"/> when an item is not a source field, an arrow and a target field.</returns>
    internal static List<PipelineComponentFieldConfiguration>? Rows(string setting, string input, IReadOnlyList<string> items)
    {
        var rows = new List<PipelineComponentFieldConfiguration>(items.Count);
        foreach (var item in items)
        {
            var split = item.IndexOf(Arrow, StringComparison.Ordinal);
            if (split < 0 || item.IndexOf(Arrow, split + Arrow.Length, StringComparison.Ordinal) >= 0)
            {
                return null;
            }

            var source = item[..split].Trim();
            var target = item[(split + Arrow.Length)..].Trim();
            if (source.Length == 0 || target.Length == 0)
            {
                return null;
            }

            rows.Add(new PipelineComponentFieldConfiguration { SettingName = setting, InputName = input, Ordinal = rows.Count, TargetField = target, SourceField = source });
        }

        return rows;
    }
}
