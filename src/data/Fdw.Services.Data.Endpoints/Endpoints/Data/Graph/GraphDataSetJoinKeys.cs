using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The join key rule of the check before save (FDW-5290): a join on a field its input does not map.
/// </summary>
/// <remarks>
/// A component works on the rows its inputs map as they arrive (FDW-2013), so a join's key is read from the mapped row. When an input states
/// mappings, its key must be the target of one of them; an input that states none passes every field and has nothing to judge. The names are the ones
/// <c>MergeJoinComponentType</c> declares, and a test holds them to it.
/// </remarks>
internal static class GraphDataSetJoinKeys
{
    /// <summary>The name of the merge join's component type.</summary>
    internal const string MergeJoin = "MergeJoin";

    /// <summary>The (key setting, input) pairs of a merge join: the left keys read the left input, the right keys the right.</summary>
    internal static readonly IReadOnlyList<(string Setting, string Input)> KeySettings = [("leftKeys", "left"), ("rightKeys", "right")];

    /// <summary>Lists each key of each merge join that its input does not map.</summary>
    /// <param name="graph">The graph, read from the draft.</param>
    /// <param name="dataSet">The name of the Graph data set.</param>
    /// <param name="logger">The logger each problem is written to.</param>
    /// <returns>The problems, each with the path of the key setting.</returns>
    internal static IEnumerable<GraphDataSetProblem> Problems(GraphPipelineConfiguration graph, string dataSet, ILogger logger)
    {
        foreach (var join in graph.Components.Where(component => string.Equals(component.ComponentType, MergeJoin, StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var (setting, input) in KeySettings)
            {
                var mapped = join.Fields
                    .Where(field => string.Equals(field.SettingName, ComponentTypeBase.MappingsSetting, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(field.InputName, input, StringComparison.OrdinalIgnoreCase))
                    .Select(field => field.TargetField)
                    .ToList();
                if (mapped.Count == 0)
                {
                    continue;
                }

                foreach (var key in join.Columns
                    .Where(column => string.Equals(column.SettingName, setting, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(column => column.Ordinal)
                    .Select(column => column.ColumnName)
                    .Where(key => !mapped.Contains(key, StringComparer.OrdinalIgnoreCase)))
                {
                    yield return GraphDataSetProblems.Of(
                        "GraphDataSetJoinFieldUnmapped", logger,
                        ResultDetails.Create("component", join.Name, "dataSet", dataSet, "field", key).With("setting", setting).With("input", input),
                        "components", join.Name, "settings", setting);
                }
            }
        }
    }
}
