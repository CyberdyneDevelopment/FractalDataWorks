using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Puts the list a run supplied into the stage a tool holds (FDW-1745): the key columns of a Distinct, the fields of a Sort.
/// </summary>
/// <remarks>
/// The stored stage is shared and cached, so each stage is copied and the copy gets a list of its own. A Sort item states its
/// direction explicitly, "City asc" or "State desc": no direction is assumed for an item that states none.
/// </remarks>
internal static class StageListApplier
{
    private const string SortFieldListKind = "SortFieldList";

    /// <summary>Gives a copy of each stage the supplied list in place of the one it was saved with.</summary>
    /// <param name="stages">The stages of the copy of the component.</param>
    /// <param name="settingKind">The kind of the list member being supplied.</param>
    /// <param name="items">The items the run supplied, in order.</param>
    /// <returns>The stages to run, or <see langword="null"/> when an item is not one the list can hold.</returns>
    internal static IList<PipelineTransformConfiguration>? Apply(IEnumerable<PipelineTransformConfiguration> stages, string settingKind, IReadOnlyList<string> items)
    {
        if (!string.Equals(settingKind, SortFieldListKind, StringComparison.Ordinal))
        {
            return [.. stages.Select(stage => WithKeyColumns(stage, items))];
        }

        var fields = SortFields(items);
        if (fields is null)
        {
            return null;
        }

        return [.. stages.Select(stage => WithSortFields(stage, fields))];
    }

    private static PipelineTransformConfiguration WithKeyColumns(PipelineTransformConfiguration stage, IReadOnlyList<string> names)
    {
        var copy = stage.Copy();
        copy.GroupByFields =
        [
            .. names.Select((name, ordinal) => new PipelineTransformGroupByFieldConfiguration { Name = name, FieldName = name, Ordinal = ordinal }),
        ];
        return copy;
    }

    private static PipelineTransformConfiguration WithSortFields(PipelineTransformConfiguration stage, List<PipelineTransformSortFieldConfiguration> fields)
    {
        var copy = stage.Copy();
        copy.SortFields = [.. fields];
        return copy;
    }

    // "City asc" and "State desc": the field is everything before the last space, the direction is the word after it.
    private static List<PipelineTransformSortFieldConfiguration>? SortFields(IReadOnlyList<string> items)
    {
        var fields = new List<PipelineTransformSortFieldConfiguration>(items.Count);
        foreach (var item in items)
        {
            var text = item.Trim();
            var split = text.LastIndexOf(' ');
            var field = split > 0 ? text[..split].Trim() : string.Empty;
            var direction = split > 0 ? DirectionOf(text[(split + 1)..]) : null;
            if (field.Length == 0 || direction is null)
            {
                return null;
            }

            fields.Add(new PipelineTransformSortFieldConfiguration { Name = field, FieldName = field, Direction = direction, Ordinal = fields.Count });
        }

        return fields;
    }

    private static string? DirectionOf(string word)
    {
        if (string.Equals(word, "asc", StringComparison.OrdinalIgnoreCase) || string.Equals(word, "ascending", StringComparison.OrdinalIgnoreCase))
        {
            return "Ascending";
        }

        return string.Equals(word, "desc", StringComparison.OrdinalIgnoreCase) || string.Equals(word, "descending", StringComparison.OrdinalIgnoreCase)
            ? "Descending"
            : null;
    }
}
