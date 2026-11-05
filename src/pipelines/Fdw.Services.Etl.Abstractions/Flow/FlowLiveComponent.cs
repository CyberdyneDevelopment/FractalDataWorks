namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A component of a flow as the live view lists it (FDW-3382): its name, the stage it runs in, and whether rows leave it.
/// </summary>
/// <param name="Name">The component's name.</param>
/// <param name="Stage">The stage number the component runs in; 0 for a component that has none.</param>
/// <param name="HasOutputs">Whether the component has output nodes: the rows it has passed on are the rows that left it, and for a component with none the rows that arrived.</param>
public sealed record FlowLiveComponent(string Name, int Stage, bool HasOutputs);
