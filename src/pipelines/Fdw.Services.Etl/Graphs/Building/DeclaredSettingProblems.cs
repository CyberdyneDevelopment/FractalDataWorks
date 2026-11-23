using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// What is wrong with the declared settings a stored component holds (FDW-6133), judged against its component type when the graph is saved and before it is built:
/// a row for a setting the component holds itself, a setting stated twice, a setting its component type declares with no data type, text that names variables for a
/// setting not read when the step runs, and text that is not its data type. The counterpart, for rows, of the per-type checks
/// <see cref="ComponentSettings.Problems(PipelineComponentConfiguration, IComponentType)"/> runs for columns.
/// </summary>
/// <remarks>A row for a setting the component type does not take at all is named by <see cref="ComponentSettings.Problems(PipelineComponentConfiguration, IComponentType)"/>.</remarks>
public static class DeclaredSettingProblems
{
    /// <summary>Lists the problems of a component's declared settings.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="kind">The component type it is judged against.</param>
    /// <returns>One problem for each thing wrong; empty when the rows agree with the component type.</returns>
    public static IEnumerable<GraphProblem> Of(PipelineComponentConfiguration component, IComponentType kind)
    {
        foreach (var repeated in component.DeclaredSettings.GroupBy(row => row.SettingName, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
        {
            yield return Problem(component, repeated.Key, $"component '{component.Name}' states the setting '{repeated.Key}' more than once");
        }

        foreach (var row in component.DeclaredSettings)
        {
            var setting = kind.Settings.FirstOrDefault(member => string.Equals(member.Name, row.SettingName, StringComparison.OrdinalIgnoreCase));
            if (setting is not null && RowProblem(component, kind, setting, row.ValueText) is { } problem)
            {
                yield return problem;
            }
        }
    }

    private static GraphProblem? RowProblem(PipelineComponentConfiguration component, IComponentType kind, StageMember setting, string text)
    {
        if (!ComponentSettingBindings.IsDeclared(kind, setting))
        {
            return Problem(component, setting.Name, $"component '{component.Name}' of component type '{kind.Name}' holds its setting '{setting.Name}' itself, not as a declared setting");
        }

        if (setting.DataType is not { } type)
        {
            return Problem(component, setting.Name, $"component type '{kind.Name}' declares its setting '{setting.Name}' with no data type, so no value for it can be read");
        }

        return VariableText.Names(text).Count > 0 ? VariablesProblem(component, setting, text) : ValueProblem(component, setting, type, text);
    }

    private static GraphProblem? ValueProblem(PipelineComponentConfiguration component, StageMember setting, IDataType type, string text) =>
        type.Convert(text).IsSuccess
            ? null
            : Problem(component, setting.Name, $"component '{component.Name}' states '{text}' for its setting '{setting.Name}', which cannot be read as {type.Name}");

    // Text that names a variable is read when the step runs, so it is judged then; a setting not read then takes only a value.
    private static GraphProblem? VariablesProblem(PipelineComponentConfiguration component, StageMember setting, string text) =>
        setting.ReadsWhenStepRuns
            ? null
            : Problem(component, setting.Name, $"component '{component.Name}' states '{text}' for its setting '{setting.Name}', which names a variable; only a setting read when the step runs may name one");

    private static GraphProblem Problem(PipelineComponentConfiguration component, string setting, string reason) =>
        new(["components", component.Name, "settings", setting], reason);
}
