using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Reads the stage each stored component runs in (FDW-2040), for the flow graph the run executes and the check judges.</summary>
internal static class ComponentStages
{
    /// <summary>Lists the stage of each component that states one.</summary>
    /// <param name="components">The stored components.</param>
    /// <returns>One stage for each component that states <c>stage</c>; a component with none is not listed, and runs first.</returns>
    internal static List<FlowStage> Of(IEnumerable<PipelineComponentConfiguration> components)
    {
        var stages = new List<FlowStage>();
        foreach (var component in components)
        {
            if (component.Stage is { } stage)
            {
                stages.Add(new FlowStage(component.Name, stage));
            }
        }

        return stages;
    }
}
