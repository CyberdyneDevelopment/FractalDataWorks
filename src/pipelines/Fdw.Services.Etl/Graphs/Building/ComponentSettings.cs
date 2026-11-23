using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Graphs.Tools;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Checks a stored component's settings against what its kind says it takes (FDW-1396): every required setting is stated and
/// none is stated that the kind does not take.
/// </summary>
/// <remarks>
/// The catalogue (<see cref="ComponentTypes"/>) is the one list of what a kind takes, so this check and the form the console
/// draws cannot disagree. A scalar setting is stated when its column is not null. A list setting is stated when it has rows.
/// </remarks>
public static class ComponentSettings
{
    /// <summary>Lists the names of the settings a component states, scalar and list.</summary>
    /// <param name="component">The stored component.</param>
    /// <returns>The names, each once.</returns>
    public static IReadOnlyList<string> Stated(PipelineComponentConfiguration component)
    {
        var names = new HashSet<string>(
            ComponentSettingBindings.Scalars.Where(setting => setting.Get(component) is not null).Select(setting => setting.Name),
            StringComparer.OrdinalIgnoreCase);
        names.UnionWith(component.SettingBindings.Select(binding => binding.SettingName));
        names.UnionWith(component.Fields.Select(field => field.SettingName));
        names.UnionWith(component.Columns.Select(column => column.SettingName));
        names.UnionWith(component.DeclaredSettings.Select(setting => setting.SettingName));
        if (component.SplitOutputs.Count > 0)
        {
            names.Add("outputs");
        }

        // Why "parameters" for the values an action gives: Execute SQL, Execute pipeline and Set variable take them as rows of the component (FDW-2041).
        if (component.Parameters.Count > 0)
        {
            names.Add("parameters");
        }

        if (component.ValueSources.Count > 0)
        {
            names.Add("valueSources");
        }

        // Why a tool states its stage's members: the tool is its stage kind, so a stored stage means every member the kind owns is
        // stated (a stage that lacks a required one was refused when it was read).
        if (component.Stages.Count > 0 && ComponentTypes.ByName(component.ComponentType) is StageToolComponentTypeBase tool)
        {
            names.UnionWith(tool.MemberNames);
        }

        return [.. names];
    }

    /// <summary>Lists every way a component's settings disagree with its kind.</summary>
    /// <param name="component">The stored component.</param>
    /// <returns>The problems: an unknown kind, each required setting left out, each setting the kind does not take.</returns>
    public static IReadOnlyList<GraphProblem> Problems(PipelineComponentConfiguration component)
    {
        var kind = ComponentTypes.ByName(component.ComponentType);
        return ReferenceEquals(kind, ComponentTypes.NotFound)
            ? [new GraphProblem(["components", component.Name], $"component '{component.Name}' is of component type '{component.ComponentType}', which is not a component type")]
            : Problems(component, kind);
    }

    /// <summary>Lists every way a component's settings disagree with a component type (FDW-6133: the component type is given, so one from another package is judged the same way).</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="kind">The component type it is judged against.</param>
    /// <returns>
    /// The problems: each required setting left out, each setting the component type does not take, and each declared setting stored wrongly (kept where the component type
    /// does not keep it, of a data type that does not exist, read from a variable it may not be, or a value that is not its data type).
    /// </returns>
    public static IReadOnlyList<GraphProblem> Problems(PipelineComponentConfiguration component, IComponentType kind)
    {
        var stated = Stated(component);
        var problems = new List<GraphProblem>();
        foreach (var setting in kind.Settings.Where(s => s.IsRequired && !stated.Contains(s.Name, StringComparer.OrdinalIgnoreCase)))
        {
            problems.Add(new GraphProblem(
                ["components", component.Name, "settings", setting.Name],
                $"component '{component.Name}' of kind '{kind.Name}' does not state the setting '{setting.Name}', which the kind requires"));
        }

        foreach (var name in stated.Where(n => !kind.Settings.Any(s => string.Equals(s.Name, n, StringComparison.OrdinalIgnoreCase))))
        {
            problems.Add(new GraphProblem(
                ["components", component.Name, "settings", name],
                $"component '{component.Name}' of kind '{kind.Name}' states the setting '{name}', which the kind does not take"));
        }

        problems.AddRange(DeclaredSettingProblems.Of(component, kind));
        problems.AddRange(OwnTextSettingProblems.Of(component, kind));
        return problems;
    }

    /// <summary>
    /// Gives a builder the value of a setting its component type declares that no column holds (FDW-6133), the counterpart of reading a column with
    /// <see cref="ScalarSetting"/>.
    /// </summary>
    /// <param name="component">The stored component.</param>
    /// <param name="kind">The component's type.</param>
    /// <param name="name">The setting's name, as the component type declares it.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// The value, or the refusal that says why there is none: ComponentSettingUndeclared, ComponentSettingHeldOnComponent, ComponentSettingDataTypeMissing,
    /// ComponentSettingNotStated or ComponentSettingValueUnreadable.
    /// </returns>
    public static IGenericResult<ComponentSettingValue> Declared(PipelineComponentConfiguration component, IComponentType kind, string name, ILogger logger)
    {
        var setting = kind.Settings.FirstOrDefault(member => string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase));
        if (setting is null)
        {
            return GenericResult<ComponentSettingValue>.Failure(GraphLog.ComponentSettingUndeclared(logger, component.Name, kind.Name, name));
        }

        if (!ComponentSettingBindings.IsDeclared(kind, setting))
        {
            return GenericResult<ComponentSettingValue>.Failure(GraphLog.ComponentSettingHeldOnComponent(logger, component.Name, kind.Name, setting.Name));
        }

        if (setting.DataType is not { } type)
        {
            return GenericResult<ComponentSettingValue>.Failure(GraphLog.ComponentSettingDataTypeMissing(logger, component.Name, kind.Name, setting.Name));
        }

        var stored = component.DeclaredSettings.FirstOrDefault(row => string.Equals(row.SettingName, setting.Name, StringComparison.OrdinalIgnoreCase));
        return stored is null
            ? GenericResult<ComponentSettingValue>.Failure(GraphLog.ComponentSettingNotStated(logger, component.Name, kind.Name, setting.Name))
            : DeclaredValue(component, kind, setting, type, stored.ValueText, logger);
    }

    /// <summary>Lists every declared setting of the given component types that states no data type (FDW-6133), checked when the host starts.</summary>
    /// <param name="kinds">The component types.</param>
    /// <param name="logger">The logger each one is written to.</param>
    /// <returns>One ComponentSettingDataTypeNotDeclared for each component type and setting; empty when every declared setting states its type.</returns>
    public static IReadOnlyList<IGenericMessage> DataTypesNotDeclared(IEnumerable<IComponentType> kinds, ILogger logger) =>
        [.. kinds.SelectMany(kind => kind.Settings
            .Where(setting => setting.DataType is null && ComponentSettingBindings.IsDeclared(kind, setting))
            .Select(setting => GraphLog.ComponentSettingDataTypeNotDeclared(logger, kind.Name, setting.Name)))];

    private static IGenericResult<ComponentSettingValue> DeclaredValue(
        PipelineComponentConfiguration component, IComponentType kind, StageMember setting, IDataType type, string text, ILogger logger)
    {
        if (setting.ReadsWhenStepRuns && VariableText.Names(text).Count > 0)
        {
            return GenericResult<ComponentSettingValue>.Success(new ComponentSettingValue(component.Name, setting.Name, type, text));
        }

        var read = type.Convert(text);
        return read.IsSuccess && read.Value is { } value
            ? GenericResult<ComponentSettingValue>.Success(new ComponentSettingValue(setting.Name, value))
            : GenericResult<ComponentSettingValue>.Failure(GraphLog.ComponentSettingValueUnreadable(logger, component.Name, kind.Name, setting.Name, text, type.Name));
    }

    /// <summary>Checks a component's settings against its kind.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the first refusal: an unknown kind, a required setting left out, or a setting the kind does not take.</returns>
    public static IGenericResult Check(PipelineComponentConfiguration component, ILogger logger)
    {
        var problems = Problems(component);
        return problems.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(GraphLog.ComponentInvalid(logger, component.Name, component.ComponentType, problems[0].Reason));
    }
}
