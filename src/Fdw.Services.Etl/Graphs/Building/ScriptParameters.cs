using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Reads the named parameters a script component states (FDW-1443). They are typed constants kept as rows of the component's
/// fields, so each is a target name and a constant of a data type; a mapping from a source field is refused, because a script
/// runs once and has no row to take a value from. A constant's text may be <c>{{Name}}</c>, a run parameter or a variable the pipeline
/// declares and no step changes (FDW-1942), read as the type the row states.
/// </summary>
public static class ScriptParameters
{
    /// <summary>The name of the setting that holds the parameters of a SQL source's script.</summary>
    public const string Setting = "parameters";

    /// <summary>Reads the parameters of one setting, in order.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="setting">The setting the parameters belong to.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>The parameters, or the failure that names the one that is incomplete or unreadable.</returns>
    public static IGenericResult<IReadOnlyList<ScriptParameter>> Of(PipelineComponentConfiguration component, string setting, FlowBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(context);
        var parameters = new List<ScriptParameter>();
        foreach (var row in component.Fields
            .Where(field => string.Equals(field.SettingName, setting, StringComparison.OrdinalIgnoreCase))
            .OrderBy(field => field.Ordinal))
        {
            if (row.SourceField is not null || string.IsNullOrWhiteSpace(row.ConstantType))
            {
                return GenericResult<IReadOnlyList<ScriptParameter>>.Failure(
                    GraphLog.FieldMappingIncomplete(context.Logger, component.Name, row.TargetField));
            }

            // A value may be a run parameter or a variable the pipeline declares, written {{Name}} (FDW-1942); it is read as the type the row states.
            var text = row.ConstantText;
            if (ParameterSettings.HoldsParameter(text))
            {
                var rendered = ComponentParameters.Text(component.Name, row.TargetField, text!, context.Parameters, context.Logger);
                if (rendered.IsFailure || rendered.Value is null)
                {
                    return rendered.ToNewResult<IReadOnlyList<ScriptParameter>>();
                }

                text = rendered.Value;
            }

            if (!GraphValues.TryRead(row.ConstantType, text, out var value))
            {
                return GenericResult<IReadOnlyList<ScriptParameter>>.Failure(
                    GraphLog.ConstantUnreadable(context.Logger, component.Name, row.TargetField, row.ConstantText, row.ConstantType));
            }

            parameters.Add(new ScriptParameter(row.TargetField, value));
        }

        return GenericResult<IReadOnlyList<ScriptParameter>>.Success(parameters);
    }
}
