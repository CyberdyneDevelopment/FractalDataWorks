using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// The stage a component runs in (FDW-2040): a component with a stage waits until every component of a lower stage, and every component with no stage, has finished.
/// A component with no stage is not listed and waits for nothing.
/// </summary>
/// <param name="Component">The component's name.</param>
/// <param name="Stage">The stage number, at least 1.</param>
public sealed record FlowStage(string Component, int Stage)
{
    /// <summary>Says what rank a component runs at: its stage number, or 0 when it states none (it runs first).</summary>
    /// <param name="stages">The stages the components state.</param>
    /// <param name="component">The component's name.</param>
    /// <returns>The rank.</returns>
    public static int RankIn(IEnumerable<FlowStage> stages, string component)
    {
        var stated = stages.FirstOrDefault(stage => string.Equals(stage.Component, component, StringComparison.OrdinalIgnoreCase));
        return stated is null ? 0 : stated.Stage;
    }
}
