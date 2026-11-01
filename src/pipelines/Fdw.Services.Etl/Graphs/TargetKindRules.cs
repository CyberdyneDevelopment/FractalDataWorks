using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Judges what a container source states about what it reads, and what a destination states about what it writes, against the kind it names
/// (FDW-3089, FDW-3193): a kind needs some settings and refuses others, a list of files goes with the Files kind of a source and a field to split by
/// with the Files kind of a destination, and nothing is assumed. The graph validator uses it when a graph is saved and the builder when the run
/// starts, so the two cannot disagree.
/// </summary>
public static class TargetKindRules
{
    /// <summary>Lists what is wrong with the settings of a container source's kind.</summary>
    /// <param name="component">The container source.</param>
    /// <returns>A problem for each setting that is wrong, by its name in the body, or an empty list.</returns>
    public static IReadOnlyList<(string Field, string Message)> Problems(PipelineComponentConfiguration component)
    {
        ArgumentNullException.ThrowIfNull(component);
        var problems = new List<(string, string)>();
        if (component.TargetKind is not { } named)
        {
            if (component.FilesList is not null)
            {
                problems.Add(("filesList", $"container source '{component.Name}' states filesList '{component.FilesList}' without a targetKind: a list of files goes with the Files kind"));
            }

            if (component.SplitField is not null)
            {
                problems.Add(("splitField", $"container source '{component.Name}' states splitField '{component.SplitField}', which only a destination takes"));
            }

            return problems;
        }

        var kind = TargetKinds.ByName(named);
        if (ReferenceEquals(kind, TargetKinds.NotFound))
        {
            problems.Add(("targetKind", $"container source '{component.Name}' names the target kind '{named}', which is not one of {nameof(TargetKinds)}"));
            return problems;
        }

        if (!kind.ForSources)
        {
            problems.Add(("targetKind", $"container source '{component.Name}' names the target kind '{named}', which is for destinations"));
            return problems;
        }

        foreach (var setting in kind.Needs)
        {
            if (string.IsNullOrWhiteSpace(Value(component, setting)))
            {
                problems.Add((setting, $"container source '{component.Name}' reads the {kind.Name} kind, which needs {setting}, and does not state it"));
            }
        }

        foreach (var setting in kind.Refuses)
        {
            if (!string.IsNullOrWhiteSpace(Value(component, setting)))
            {
                problems.Add((setting, $"container source '{component.Name}' reads the {kind.Name} kind, which does not take {setting}, and states it"));
            }
        }

        return problems;
    }

    /// <summary>Lists what is wrong with the settings of a destination's kind.</summary>
    /// <param name="component">The container or data set destination.</param>
    /// <returns>A problem for each setting that is wrong, by its name in the body, or an empty list.</returns>
    public static IReadOnlyList<(string Field, string Message)> WriteProblems(PipelineComponentConfiguration component)
    {
        ArgumentNullException.ThrowIfNull(component);
        var label = $"destination '{component.Name}'";
        var problems = new List<(string, string)>();
        if (component.TargetKind is not { } named)
        {
            foreach (var setting in new[] { "physicalPath", "physicalName", "filesList", "splitField" })
            {
                if (!string.IsNullOrWhiteSpace(Value(component, setting)))
                {
                    problems.Add((setting, $"{label} states {setting} '{Value(component, setting)}' without a targetKind: it goes with the File or Files kind"));
                }
            }

            return problems;
        }

        var kind = TargetKinds.ByName(named);
        if (ReferenceEquals(kind, TargetKinds.NotFound))
        {
            problems.Add(("targetKind", $"{label} names the target kind '{named}', which is not one of {nameof(TargetKinds)}"));
            return problems;
        }

        if (!kind.ForDestinations)
        {
            problems.Add(("targetKind", $"{label} names the target kind '{named}', which is for sources"));
            return problems;
        }

        foreach (var setting in kind.WritesNeed.Where(setting => string.IsNullOrWhiteSpace(Value(component, setting))))
        {
            problems.Add((setting, $"{label} writes the {kind.Name} kind, which needs {setting}, and does not state it"));
        }

        foreach (var setting in kind.WritesRefuse.Where(setting => !string.IsNullOrWhiteSpace(Value(component, setting))))
        {
            problems.Add((setting, $"{label} writes the {kind.Name} kind, which does not take {setting}, and states it"));
        }

        return problems;
    }

    // The settings a kind may name; a name that is not one is a mistake in a kind, not in a graph.
    private static string? Value(PipelineComponentConfiguration component, string setting) => setting switch
    {
        "container" => component.ContainerName,
        "dataSet" => component.DataSetName,
        "physicalPath" => component.PhysicalPath,
        "physicalName" => component.PhysicalName,
        "filesList" => component.FilesList,
        "splitField" => component.SplitField,
        _ => throw new ArgumentOutOfRangeException(nameof(setting), setting, "A target kind names a setting the component does not have.")
    };
}
