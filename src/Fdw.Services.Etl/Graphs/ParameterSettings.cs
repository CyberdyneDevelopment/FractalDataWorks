using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Building;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The settings of a source or destination that may hold a run parameter (FDW-1689): the text that names what it reads or writes, the data
/// set, or the data store, path and container (the file), written <c>{{Name}}</c>. The name may also be a variable the pipeline declares
/// and no step changes (FDW-1942).
/// </summary>
/// <remarks>
/// The catalogue says which settings of which kinds take a parameter (FDW-1768), and this reads it, so the run that renders them, the check that refuses an undeclared
/// name and the description that cannot know the fields of a source named by a parameter agree. Every other setting stays fixed text.
/// </remarks>
public static class ParameterSettings
{
    /// <summary>Lists the settings of the component that name something and may hold a parameter, with the text each holds. A setting that is read when its step runs is not listed here; see <see cref="LateOf"/>.</summary>
    /// <param name="component">The stored component.</param>
    /// <returns>The setting name (as the graph payload names it) and its text; none for a component that takes no parameter.</returns>
    public static IReadOnlyList<(string Setting, string Text)> Of(PipelineComponentConfiguration component) => Settings(component, late: false);

    /// <summary>Lists the settings of the component that are rendered when the component's step runs (FDW-1958), with the text each holds.</summary>
    /// <param name="component">The stored component.</param>
    /// <returns>The setting name and its text; none for a component that has no such setting stated.</returns>
    public static IReadOnlyList<(string Setting, string Text)> LateOf(PipelineComponentConfiguration component) => Settings(component, late: true);

    private static List<(string Setting, string Text)> Settings(PipelineComponentConfiguration component, bool late)
    {
        ArgumentNullException.ThrowIfNull(component);
        var found = new List<(string Setting, string Text)>();
        foreach (var member in ComponentTypes.ByName(component.ComponentType).Settings.Where(member => member.TakesParameterText && member.ReadsWhenStepRuns == late))
        {
            if (ComponentSettingBindings.Find(member.Name)?.Get(component) is string text && !string.IsNullOrWhiteSpace(text))
            {
                found.Add((member.Name, text));
            }
        }

        return found;
    }

    /// <summary>
    /// Lists the mappings on a component's inputs whose constant names a parameter, a variable or a run fact in double braces (FDW-2119): a single value
    /// the mapping puts in its target field for every row. Like the physical name of a source, it is read when the component's stage starts.
    /// </summary>
    /// <param name="component">The stored component.</param>
    /// <returns>The mappings, in order; none for a component whose mapping constants are plain values.</returns>
    public static IReadOnlyList<PipelineComponentFieldConfiguration> MappingConstants(PipelineComponentConfiguration component)
    {
        ArgumentNullException.ThrowIfNull(component);
        return [.. component.Fields
            .Where(field => string.Equals(field.SettingName, ComponentTypeBase.MappingsSetting, StringComparison.OrdinalIgnoreCase) && HoldsParameter(field.ConstantText))
            .OrderBy(field => field.Ordinal)];
    }

    /// <summary>Whether a setting of a component takes its value from a run parameter.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="setting">The setting's name in the catalogue.</param>
    /// <returns><see langword="true"/> when the component binds the setting.</returns>
    public static bool IsBound(PipelineComponentConfiguration component, string setting)
    {
        ArgumentNullException.ThrowIfNull(component);
        return component.SettingBindings.Any(binding => string.Equals(binding.SettingName, setting, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Whether a setting's text holds at least one run parameter.</summary>
    /// <param name="text">The text, or none.</param>
    /// <returns><see langword="true"/> when the text names a parameter in double braces.</returns>
    public static bool HoldsParameter(string? text) => !string.IsNullOrEmpty(text) && VariableText.Names(text).Count > 0;
}
