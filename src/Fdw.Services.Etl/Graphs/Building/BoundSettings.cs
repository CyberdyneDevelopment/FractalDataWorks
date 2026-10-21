using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Gives the settings that take their value from a run parameter that value when the run starts (FDW-1722): a component or step whose
/// setting is bound is built from a copy that holds the value, converted to the setting's type.
/// </summary>
/// <remarks>
/// The stored component or step is never changed: the configuration is shared and cached. A parameter the run was started without a
/// value for, or whose value is not of the setting's kind, fails the run before any row is read, naming what takes it. Nothing is
/// substituted.
/// </remarks>
public static class BoundSettings
{
    /// <summary>Applies the bindings of a component.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="context">What the run builds with; its parameters are the run's typed values.</param>
    /// <returns>The component to build from (the stored one when nothing is bound, otherwise a copy), or the failure that names the parameter.</returns>
    public static IGenericResult<PipelineComponentConfiguration> Apply(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Apply(component, context.Parameters, context.Logger);
    }

    /// <summary>Applies the bindings of a component from the run's typed values, without a build context (FDW-1891).</summary>
    /// <remarks>
    /// Why this reads no build context: the same rendering decides what a run reads, and the run's roles are judged for exactly that before
    /// the run is queued, when only the values and a logger exist.
    /// </remarks>
    /// <param name="component">The stored component.</param>
    /// <param name="parameters">The run's typed values by parameter name.</param>
    /// <param name="logger">The logger the failures are written to.</param>
    /// <returns>The component to build from (the stored one when nothing is bound, otherwise a copy), or the failure that names the parameter.</returns>
    public static IGenericResult<PipelineComponentConfiguration> Apply(
        PipelineComponentConfiguration component, IReadOnlyDictionary<string, object?> parameters, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(logger);
        if (component.SettingBindings.Count == 0)
        {
            return GenericResult<PipelineComponentConfiguration>.Success(component);
        }

        var copy = component.Copy();
        foreach (var binding in component.SettingBindings)
        {
            var applied = ApplyOne(component, copy, binding, parameters, logger);
            if (applied.IsFailure)
            {
                return applied.ToNewResult<PipelineComponentConfiguration>();
            }
        }

        return GenericResult<PipelineComponentConfiguration>.Success(copy);
    }

    private static IGenericResult ApplyOne(
        PipelineComponentConfiguration component,
        PipelineComponentConfiguration copy,
        PipelineComponentSettingBindingConfiguration binding,
        IReadOnlyDictionary<string, object?> parameters,
        ILogger logger)
    {
        if (ComponentSettingBindings.Find(binding.SettingName) is { } setting)
        {
            var typed = Value(component.Name, setting.Name, setting.Kind, binding.ParameterName, parameters, logger);
            if (typed.IsFailure || typed.Value is null)
            {
                return typed;
            }

            setting.Set(copy, typed.Value);
            return GenericResult.Success();
        }

        return SettingBindingRules.ListSetting(ComponentTypes.ByName(component.ComponentType), binding.SettingName) is { } list
            ? ApplyList(component, copy, list, binding.ParameterName, parameters, logger)
            : GenericResult.Failure(GraphLog.ParameterNotConvertible(logger, component.Name, binding.SettingName, binding.ParameterName, "a setting"));
    }

    // A list is replaced whole on the copy: the stored component and its stage are shared and cached, so the copy gets a list of its own.
    private static IGenericResult ApplyList(
        PipelineComponentConfiguration component,
        PipelineComponentConfiguration copy,
        StageMember list,
        string parameter,
        IReadOnlyDictionary<string, object?> parameters,
        ILogger logger)
    {
        var given = parameters.FirstOrDefault(pair => string.Equals(pair.Key, parameter, StringComparison.OrdinalIgnoreCase));
        if (given.Key is null || given.Value is null)
        {
            return GenericResult.Failure(GraphLog.ParameterNotGiven(logger, component.Name, list.Name, parameter));
        }

        if (given.Value is not IReadOnlyList<object> { Count: > 0 } items || items.Any(item => item is not string { Length: > 0 }))
        {
            return GenericResult.Failure(GraphLog.ParameterNotConvertible(logger, component.Name, list.Name, parameter, "a list of column names"));
        }

        var names = items.Cast<string>().ToList();
        if (list.TakesPairList)
        {
            return ApplyPairs(component, copy, list, parameter, names, logger);
        }

        if (SettingBindingRules.IsStageListKind(list.Kind))
        {
            var stages = StageListApplier.Apply(copy.Stages, list.Kind, names);
            if (stages is null)
            {
                return GenericResult.Failure(GraphLog.ParameterNotConvertible(
                    logger, component.Name, list.Name, parameter, "a list of fields, each a field name and then asc or desc"));
            }

            copy.Stages = stages;
            return GenericResult.Success();
        }

        var ordinal = 0;
        copy.Columns =
        [
            .. component.Columns.Where(column => !string.Equals(column.SettingName, list.Name, StringComparison.OrdinalIgnoreCase)),
            .. names.Select(name => new PipelineComponentColumnConfiguration { SettingName = list.Name, Ordinal = ordinal++, ColumnName = name }),
        ];
        return GenericResult.Success();
    }

    // The mappings of an input may come from the run as pairs; the copy's rows for the setting are replaced whole (the stored rows are shared and cached).
    private static IGenericResult ApplyPairs(
        PipelineComponentConfiguration component,
        PipelineComponentConfiguration copy,
        StageMember list,
        string parameter,
        IReadOnlyList<string> pairs,
        ILogger logger)
    {
        // Why one input only: the list a run supplies names no input, so it can only be for a component that has a single one.
        var inputs = ComponentTypes.ByName(component.ComponentType).Inputs;
        if (inputs.Count != 1 || inputs[0].IsNamedByUser)
        {
            return GenericResult.Failure(GraphLog.ParameterNotConvertible(logger, component.Name, list.Name, parameter, "a component with one input"));
        }

        var rows = FieldMapPairs.Rows(list.Name, inputs[0].Name, pairs);
        if (rows is null)
        {
            return GenericResult.Failure(GraphLog.ParameterNotConvertible(
                logger, component.Name, list.Name, parameter, "a list of field maps, each a source field, an arrow (->) and a target field"));
        }

        copy.Fields =
        [
            .. component.Fields.Where(field => !string.Equals(field.SettingName, list.Name, StringComparison.OrdinalIgnoreCase)),
            .. rows,
        ];
        return GenericResult.Success();
    }

    private static IGenericResult<object> Value(
        string owner, string setting, string settingKind, string parameter, IReadOnlyDictionary<string, object?> parameters, ILogger logger)
    {
        var given = parameters.FirstOrDefault(pair => string.Equals(pair.Key, parameter, StringComparison.OrdinalIgnoreCase));
        if (given.Key is null || given.Value is null)
        {
            return GenericResult<object>.Failure(GraphLog.ParameterNotGiven(logger, owner, setting, parameter));
        }

        return given.Value is not IReadOnlyList<object> && SettingBindingRules.TryConvert(settingKind, given.Value, out var typed) && typed is not null
            ? GenericResult<object>.Success(typed)
            : GenericResult<object>.Failure(GraphLog.ParameterNotConvertible(logger, owner, setting, parameter, SettingKindWords(settingKind)));
    }

    private static string SettingKindWords(string settingKind) => settingKind switch
    {
        "Number" => "a whole number",
        "Boolean" => "true or false",
        _ => "text",
    };
}
