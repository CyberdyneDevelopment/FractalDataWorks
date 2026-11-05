using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A <see cref="IFlowRunProgress"/> that is also told how long a component ran (FDW-3865). The executor calls this overload when the progress offers it, and the plain
/// one otherwise.
/// </summary>
public interface IFlowRunTimedProgress : IFlowRunProgress
{
    /// <summary>Tells the progress that a component has finished, with how long it ran.</summary>
    /// <param name="component">The component.</param>
    /// <param name="links">The count of every link in the flow at this moment.</param>
    /// <param name="runTime">How long the component ran, from its start to its end.</param>
    void ComponentFinished(IFlowComponent component, IReadOnlyList<FlowLinkCount> links, TimeSpan runTime);
}
