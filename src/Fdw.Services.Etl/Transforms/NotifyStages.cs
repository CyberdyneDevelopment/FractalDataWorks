using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Finds the Notify stages of a stage tree, however deeply nested, in execution order (FDW-1120).
/// </summary>
/// <remarks>
/// The one place that reads a tree for its Notify stages: a run gathers them to fire them at their run points, and a save
/// reads them to confirm each stage's rule exists. A disabled stage, and everything inside it, is left out.
/// </remarks>
public static class NotifyStages
{
    private const string Kind = "Notify";

    /// <summary>Finds the enabled Notify stages of a stage list and of the paths of its Branch stages.</summary>
    /// <param name="stages">The stages of the pipeline; null is an empty list.</param>
    /// <returns>The Notify stages, each with the path that addresses it.</returns>
    public static IReadOnlyList<NotifyStage> Of(IEnumerable<PipelineTransformConfiguration>? stages)
    {
        var found = new List<NotifyStage>();
        Gather(stages, [], found);
        return found;
    }

    private static void Gather(IEnumerable<PipelineTransformConfiguration>? stages, IReadOnlyList<string> parentPath, List<NotifyStage> found)
    {
        foreach (var stage in StageRunner.Ordered(stages))
        {
            var stagePath = parentPath.Append(stage.Name).ToList();
            if (string.Equals(stage.OperationType, Kind, StringComparison.OrdinalIgnoreCase))
            {
                found.Add(new NotifyStage(stagePath, stage.Name, stage.NotificationRuleName, stage.RunPoint, stage.NotificationMessage));
            }

            foreach (var path in stage.Paths)
            {
                Gather(path.Stages, stagePath.Append(path.Name).ToList(), found);
            }
        }
    }
}
