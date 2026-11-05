using System;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What the executor tells the live view about one flow graph while it runs (FDW-3382). Disposing it removes the graph from the view.
/// </summary>
public interface IFlowLiveGraph : IDisposable
{
    /// <summary>Notes that a component has started: it has been released by its stage and by the completion lines it waits for.</summary>
    /// <param name="component">The component's name.</param>
    void ComponentStarted(string component);

    /// <summary>Notes that a component has ended, however it ended.</summary>
    /// <param name="component">The component's name.</param>
    /// <param name="succeeded">Whether the component's run succeeded.</param>
    void ComponentEnded(string component, bool succeeded);
}
