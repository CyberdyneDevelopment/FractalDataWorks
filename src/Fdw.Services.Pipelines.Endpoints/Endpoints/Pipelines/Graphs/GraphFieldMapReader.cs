using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Reads the field mappings of a component setting of kind <c>FieldMaps</c> (FDW-1396, FDW-2000): a target field from a source field or a typed
/// constant, and for a setting that takes them a type to convert to, a default, a required flag and a chain of named Transformations.
/// </summary>
/// <remarks>
/// Why a setting that does not take the four parts refuses them: a Lookup's brought columns and a Union all's mappings have no use for them, and a part that is
/// stated and not applied would be a mapping that quietly does less than it says.
/// </remarks>
internal static class GraphFieldMapReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Reads the mappings of one setting into the component.</summary>
    /// <param name="component">The component being read.</param>
    /// <param name="setting">The setting, from the component's catalogue.</param>
    /// <param name="value">What the request states for it.</param>
    /// <param name="transformationIds">The id of each transformation the mappings name, by name.</param>
    /// <returns>Success, or the refusal that names the component, the setting and the mapping.</returns>
    internal static IGenericResult Read(
        PipelineComponentConfiguration component, StageMember setting, JsonElement value, IReadOnlyDictionary<string, Guid> transformationIds)
    {
        var name = setting.Name;

        // An input's mappings may come from a list run parameter (FDW-1745); a setting of this kind whose maps mean something else cannot.
        if (GraphSettingValue.ParameterOf(value) is { } parameter)
        {
            if (!setting.TakesPairList)
            {
                return Refused(component, name, "cannot take its field maps from a run parameter");
            }

            component.SettingBindings.Add(new PipelineComponentSettingBindingConfiguration { SettingName = name, ParameterName = parameter });
            return GenericResult.Success();
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return Refused(component, name, "must be a list of field mappings");
        }

        var maps = JsonSerializer.Deserialize<List<GraphFieldMapPayload>>(value.GetRawText(), JsonOptions);
        if (maps is null)
        {
            return Refused(component, name, "must be a list of field mappings");
        }

        var kind = ComponentTypes.ByName(component.ComponentType);
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var ordinal = 0; ordinal < maps.Count; ordinal++)
        {
            var map = maps[ordinal];
            if ((map.Source is null) == (map.Constant is null))
            {
                return Refused(component, name, $"maps '{map.Target}' from either a source field or a constant, not both and not neither");
            }

            var misplaced = setting.TakesConversions ? InputProblem(kind, map, targets) : null;
            if (misplaced is not null)
            {
                return Refused(component, name, misplaced);
            }

            var row = new PipelineComponentFieldConfiguration
            {
                SettingName = name,
                InputName = map.Input,
                Ordinal = ordinal,
                TargetField = map.Target,
                SourceField = map.Source,
                ConstantType = map.Constant?.Type,
                ConstantText = map.Constant?.Value,
            };
            var refusal = ReadConversion(setting, map, row, transformationIds);
            if (refusal is not null)
            {
                return Refused(component, name, $"maps '{map.Target}' and {refusal}");
            }

            component.Fields.Add(row);
        }

        return GenericResult.Success();
    }

    // A mapping names the input it is on (FDW-2013), that input is one the kind has or one the graph's links will name, and each input maps a target once.
    private static string? InputProblem(IComponentType kind, GraphFieldMapPayload map, HashSet<string> targets)
    {
        if (string.IsNullOrWhiteSpace(map.Input))
        {
            return $"maps '{map.Target}' without naming its input";
        }

        if (!kind.Inputs.Any(spec => spec.IsNamedByUser || string.Equals(spec.Name, map.Input, StringComparison.OrdinalIgnoreCase)))
        {
            return $"maps '{map.Target}' on the input '{map.Input}', which a {kind.Name} does not have";
        }

        return targets.Add($"{map.Input}/{map.Target}") ? null : $"maps '{map.Target}' twice on the input '{map.Input}'";
    }

    // The type, default, required flag and chain a mapping states. Returns why they cannot be stored, or null.
    private static string? ReadConversion(StageMember setting, GraphFieldMapPayload map, PipelineComponentFieldConfiguration row, IReadOnlyDictionary<string, Guid> transformationIds)
    {
        var stated = map.TargetType is not null || map.DefaultValue is not null || map.IsRequired is not null || map.Transformations is { Count: > 0 };
        if (!stated)
        {
            return null;
        }

        if (!setting.TakesConversions)
        {
            return "states a type, a default, a required flag or transformations, which this setting does not take";
        }

        if (map.TargetType is { } type && !FieldTypeConversion.IsSupported(type))
        {
            return $"the type '{type}' is not one a mapping converts to ({string.Join(", ", FieldTypeConversion.Supported)})";
        }

        if (map.TargetType is { } named && map.DefaultValue is { } text && map.Transformations is not { Count: > 0 }
            && !FieldTypeConversion.TryConvert(text, named, out _, out _))
        {
            return $"the default '{text}' is not a value of {named}";
        }

        row.TargetType = map.TargetType;
        row.DefaultText = map.DefaultValue;
        row.IsRequired = map.IsRequired is true;
        return map.Transformations is { Count: > 0 } steps ? ReadChain(row, steps, transformationIds) : null;
    }

    // The steps are counted from 1 without gaps, and each names its Transformation by name or by id, never both and never neither. A name that
    // is not found means the Transformation does not exist; it is refused, not skipped.
    private static string? ReadChain(PipelineComponentFieldConfiguration row, IList<PipelineFieldTransformationReference> steps, IReadOnlyDictionary<string, Guid> transformationIds)
    {
        var ordered = steps.OrderBy(step => step.Ordinal).ToList();
        if (!ordered.Select(step => step.Ordinal).SequenceEqual(Enumerable.Range(1, ordered.Count)))
        {
            return $"its transformations are not numbered 1 to {ordered.Count} without gaps";
        }

        foreach (var step in ordered)
        {
            var name = string.IsNullOrWhiteSpace(step.Transformation) ? null : step.Transformation;
            if (name is not null && step.TransformationId is not null)
            {
                return $"names transformation step {step.Ordinal} by both its name and its id";
            }

            if (name is null && step.TransformationId is null)
            {
                return $"names transformation step {step.Ordinal} by neither its name nor its id";
            }

            Guid id;
            if (name is null)
            {
                id = step.TransformationId!.Value;
            }
            else if (!transformationIds.TryGetValue(name, out id))
            {
                return $"names the transformation '{name}' at step {step.Ordinal}, which does not exist";
            }

            row.TransformationChain.Add(new PipelineComponentFieldTransformationConfiguration
            {
                Name = name ?? id.ToString("D"),
                TransformationId = id,
                Ordinal = step.Ordinal,
            });
        }

        return null;
    }

    private static IGenericResult Refused(PipelineComponentConfiguration component, string setting, string reason) =>
        GraphRefusal.Of<bool>($"component '{component.Name}' setting '{setting}' {reason}");
}
