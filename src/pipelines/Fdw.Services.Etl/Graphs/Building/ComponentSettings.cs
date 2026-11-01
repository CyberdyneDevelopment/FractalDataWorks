using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
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
        ArgumentNullException.ThrowIfNull(component);
        var names = new HashSet<string>(
            ComponentSettingBindings.Scalars.Where(setting => setting.Get(component) is not null).Select(setting => setting.Name),
            StringComparer.OrdinalIgnoreCase);
        names.UnionWith(component.SettingBindings.Select(binding => binding.SettingName));
        names.UnionWith(component.Fields.Select(field => field.SettingName));
        names.UnionWith(component.Columns.Select(column => column.SettingName));
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
        ArgumentNullException.ThrowIfNull(component);
        var kind = ComponentTypes.ByName(component.ComponentType);
        if (ReferenceEquals(kind, ComponentTypes.NotFound))
        {
            return [new GraphProblem(["components", component.Name], $"component '{component.Name}' is of kind '{component.ComponentType}', which is not a component kind")];
        }

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

        return problems;
    }

    /// <summary>Checks a component's settings against its kind.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the first refusal: an unknown kind, a required setting left out, or a setting the kind does not take.</returns>
    public static IGenericResult Check(PipelineComponentConfiguration component, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(logger);
        var problems = Problems(component);
        return problems.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(GraphLog.ComponentInvalid(logger, component.Name, component.ComponentType, problems[0].Reason));
    }
}
