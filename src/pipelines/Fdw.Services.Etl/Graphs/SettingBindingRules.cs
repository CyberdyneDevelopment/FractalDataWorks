using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The rules for a setting that takes its value from a run parameter (FDW-1722): which settings may, what type of parameter fits each
/// kind of setting, how a parameter's value becomes the setting's, and what is wrong with a graph's bindings.
/// </summary>
/// <remarks>
/// One place says it, so the check at save, the run that applies the bindings and the form that offers them cannot disagree. A setting
/// that names a node, a component or kept rows shapes the graph, which links and other components refer to by name, so it cannot be chosen by a run. A step's
/// variable names can: the checks that need them run again when the run starts (FDW-1733).
/// </remarks>
public static class SettingBindingRules
{
    /// <summary>Whether a run may supply the setting of a component.</summary>
    /// <param name="kind">The component's kind, which states in the catalogue which of its settings shape the graph (FDW-1768).</param>
    /// <param name="setting">The setting's name in the catalogue.</param>
    /// <returns><see langword="true"/> unless the kind states that the setting shapes the graph.</returns>
    public static bool CanBindComponentSetting(IFlowComponentShape kind, string setting)
    {
        ArgumentNullException.ThrowIfNull(kind);
        return !kind.Settings.Any(member => member.ShapesGraph && string.Equals(member.Name, setting, StringComparison.OrdinalIgnoreCase));
    }

    private const string FieldMapPairsKind = "FieldMapPairs";

    /// <summary>Says what a setting holds when a run supplies it: its own kind, or a list of field map pairs for a setting that takes one (FDW-1745).</summary>
    /// <param name="setting">The setting of a component kind.</param>
    /// <returns>The kind the binding rules judge the setting by.</returns>
    public static string BoundKind(StageMember setting)
    {
        ArgumentNullException.ThrowIfNull(setting);
        return setting.TakesPairList ? FieldMapPairsKind : setting.Kind;
    }

    /// <summary>Finds the list setting of a component kind that a run may supply (FDW-1745).</summary>
    /// <param name="kind">The component's kind.</param>
    /// <param name="setting">The setting's name in the catalogue.</param>
    /// <returns>The setting when the kind lists a setting of that name that holds a list of column names; otherwise <see langword="null"/>.</returns>
    public static StageMember? ListSetting(IFlowComponentShape kind, string setting)
    {
        ArgumentNullException.ThrowIfNull(kind);
        return kind.Settings.FirstOrDefault(member =>
            IsListKind(BoundKind(member)) && string.Equals(member.Name, setting, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Says whether a kind of setting holds a list a run supplies whole, as a list parameter.</summary>
    /// <param name="settingKind">What the setting holds.</param>
    /// <returns><see langword="true"/> for a list of column names, and for the key columns of a stage tool.</returns>
    public static bool IsListKind(string settingKind) =>
        string.Equals(settingKind, "ColumnList", StringComparison.Ordinal)
        || string.Equals(settingKind, FieldMapPairsKind, StringComparison.Ordinal)
        || IsStageListKind(settingKind);

    /// <summary>Says whether a kind of setting is a list that lives in the stage a tool holds, not in the component's own column rows (FDW-1745).</summary>
    /// <param name="settingKind">What the setting holds.</param>
    /// <returns><see langword="true"/> for the key columns of a Distinct tool and the fields of a Sort tool.</returns>
    public static bool IsStageListKind(string settingKind) =>
        string.Equals(settingKind, "Distinct", StringComparison.Ordinal) || string.Equals(settingKind, "SortFieldList", StringComparison.Ordinal);

    /// <summary>Says which <c>DataTypes</c> option a parameter must have to supply a setting of a kind.</summary>
    /// <param name="settingKind">What the setting holds: <c>Text</c>, <c>Number</c>, <c>Boolean</c> or <c>ColumnList</c>.</param>
    /// <returns>The data type name (of each item, for a list), or an empty text for a kind no parameter can supply.</returns>
    public static string ParameterTypeFor(string settingKind) => settingKind switch
    {
        "Text" => "String",
        "Number" => "Int32",
        "Boolean" => "Bool",
        "ColumnList" => "String",
        "Distinct" => "String",
        "SortFieldList" => "String",
        FieldMapPairsKind => "String",
        _ => string.Empty,
    };

    /// <summary>Makes a setting's value out of the value a run was given for a parameter.</summary>
    /// <param name="settingKind">What the setting holds: <c>Text</c>, <c>Number</c> or <c>Boolean</c>.</param>
    /// <param name="value">The parameter's typed value.</param>
    /// <param name="typed">The value as the setting holds it.</param>
    /// <returns><see langword="true"/> when the value is of the setting's kind or converts to it.</returns>
    public static bool TryConvert(string settingKind, object value, out object? typed)
    {
        ArgumentNullException.ThrowIfNull(value);
        typed = null;
        switch (settingKind)
        {
            case "Text":
                typed = Convert.ToString(value, CultureInfo.InvariantCulture);
                return typed is not null;
            case "Number" when value is IConvertible number:
                return TryInt(number, out typed);
            case "Boolean" when value is bool flag:
                typed = flag;
                return true;
            default:
                return false;
        }
    }

    private static bool TryInt(IConvertible number, out object? typed)
    {
        try
        {
            typed = number.ToInt32(CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            typed = null;
            return false;
        }
    }

    /// <summary>Lists every way the bindings of a graph's components are wrong.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <returns>The problems, each with its path: a setting that does not exist or cannot be bound, a parameter the pipeline does not declare or of the wrong type, a setting that is bound twice or also holds a value.</returns>
    public static IReadOnlyList<GraphProblem> Problems(GraphPipelineConfiguration graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var parameters = new Dictionary<string, (string Type, bool IsList)>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in graph.Parameters.Where(parameter => !string.IsNullOrWhiteSpace(parameter.Name)))
        {
            // Two parameters with one name are reported once, by GraphComponentValidator; here the first stands for the name so a binding is
            // judged against something instead of this check throwing on the duplicate.
            parameters.TryAdd(parameter.Name, (parameter.ParameterType, parameter.IsList));
        }

        var problems = new List<GraphProblem>();
        foreach (var component in graph.Components)
        {
            problems.AddRange(ComponentProblems(component, parameters));
        }

        return problems;
    }

    private static IEnumerable<GraphProblem> ComponentProblems(PipelineComponentConfiguration component, Dictionary<string, (string Type, bool IsList)> parameters)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kind = ComponentTypes.ByName(component.ComponentType);
        foreach (var binding in component.SettingBindings)
        {
            var reason = ComponentReason(component, kind, binding, seen, parameters);
            if (reason is not null)
            {
                yield return new GraphProblem(["components", component.Name, "settings", binding.SettingName], reason);
            }
        }
    }

    // The setting a binding names, with whether the component already states a value for it: a single-value setting or a list of columns.
    private static (string Name, string Kind, bool Stated)? BoundSetting(PipelineComponentConfiguration component, IFlowComponentShape kind, string settingName)
    {
        if (ComponentSettingBindings.Find(settingName) is { } scalar)
        {
            return (scalar.Name, scalar.Kind, scalar.Get(component) is not null);
        }

        if (ListSetting(kind, settingName) is { } list)
        {
            var stated = list.TakesPairList
                ? component.Fields.Any(field => string.Equals(field.SettingName, list.Name, StringComparison.OrdinalIgnoreCase))
                : component.Columns.Any(column => string.Equals(column.SettingName, list.Name, StringComparison.OrdinalIgnoreCase));
            return (list.Name, BoundKind(list), stated);
        }

        return null;
    }

    // What is wrong with one binding of a component: a single-value setting or a list of columns, each judged the same way (FDW-1745).
    private static string? ComponentReason(
        PipelineComponentConfiguration component,
        IFlowComponentShape kind,
        PipelineComponentSettingBindingConfiguration binding,
        HashSet<string> seen,
        Dictionary<string, (string Type, bool IsList)> parameters)
    {
        var target = BoundSetting(component, kind, binding.SettingName);
        if (target is not { } setting)
        {
            return $"component '{component.Name}' binds '{binding.SettingName}', which is not a single-value setting";
        }

        return !CanBindComponentSetting(kind, setting.Name) ? $"component '{component.Name}' binds '{setting.Name}', which shapes the graph and cannot be chosen by a run"
            : !seen.Add(setting.Name) ? $"component '{component.Name}' binds '{setting.Name}' more than once"
            : setting.Stated ? $"component '{component.Name}' states a value for '{setting.Name}' and also binds it to a parameter"
            : ParameterReason(parameters, binding.ParameterName, setting.Name, setting.Kind);
    }

    private static string? ParameterReason(Dictionary<string, (string Type, bool IsList)> parameters, string parameter, string setting, string settingKind)
    {
        if (!parameters.TryGetValue(parameter, out var declared))
        {
            return $"the setting '{setting}' takes its value from the parameter '{parameter}', but the pipeline declares no parameter '{parameter}'";
        }

        var wantsList = IsListKind(settingKind);
        if (declared.IsList != wantsList)
        {
            return wantsList
                ? $"the setting '{setting}' holds a list and takes a list parameter, but '{parameter}' is declared as a single {declared.Type}"
                : $"the setting '{setting}' holds {settingKind.ToLowerInvariant()} and takes a single-value parameter, but '{parameter}' is declared as a list of {declared.Type}";
        }

        var expected = ParameterTypeFor(settingKind);
        return string.Equals(declared.Type, expected, StringComparison.OrdinalIgnoreCase)
            ? null
            : $"the setting '{setting}' holds {(wantsList ? "a list" : settingKind.ToLowerInvariant())} and takes a parameter of type {expected}, but '{parameter}' is declared as {declared.Type}";
    }
}
