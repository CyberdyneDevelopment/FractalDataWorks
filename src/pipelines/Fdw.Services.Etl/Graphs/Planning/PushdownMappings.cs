using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// Pushes the mappings on a component's input (FDW-6969): a plain rename, a field of the input under a new name, is a projection alias; any other
/// mapping (a constant, a conversion, a default, a required value, a chain of transformations) is work the store does not do.
/// </summary>
public static class PushdownMappings
{
    /// <summary>Applies the mappings of one input to the side that feeds it.</summary>
    /// <param name="component">The component the input belongs to.</param>
    /// <param name="input">The input's name.</param>
    /// <param name="setting">The setting the mappings belong to: the input mappings, or a lookup's bring.</param>
    /// <param name="side">The side that feeds it.</param>
    /// <returns>The fields the mappings give, in order; the side's own fields when the input states none; or why they cannot be pushed.</returns>
    public static (IReadOnlyList<PushdownColumn> Fields, string Reason) Apply(
        PipelineComponentConfiguration component, string? input, string setting, PushdownSide side)
    {
        var rows = component.Fields
            .Where(field => string.Equals(field.SettingName, setting, StringComparison.OrdinalIgnoreCase)
                && string.Equals(field.InputName, input, StringComparison.OrdinalIgnoreCase))
            .OrderBy(field => field.Ordinal)
            .ToList();
        if (rows.Count == 0)
        {
            return (string.Equals(setting, ComponentTypeBase.MappingsSetting, StringComparison.OrdinalIgnoreCase) ? side.Fields : [], string.Empty);
        }

        var fields = new List<PushdownColumn>(rows.Count);
        foreach (var row in rows)
        {
            if (row.SourceField is not { } source || row.ConstantType is not null || row.TargetType is not null || row.DefaultText is not null
                || row.IsRequired || row.TransformationChain.Count > 0)
            {
                return ([], $"its mapping of '{row.TargetField}' is not a plain rename of a field");
            }

            if (side.Field(source) is not { } read)
            {
                return ([], $"its mapping of '{row.TargetField}' names '{source}', which its input does not carry");
            }

            if (!PlainIdentifiers.IsPlain(row.TargetField) || fields.Exists(field => string.Equals(field.Name, row.TargetField, StringComparison.OrdinalIgnoreCase)))
            {
                return ([], $"its mapping names '{row.TargetField}' twice or as a name a statement does not write as it is");
            }

            fields.Add(read with { Name = row.TargetField });
        }

        return (fields, string.Empty);
    }
}
