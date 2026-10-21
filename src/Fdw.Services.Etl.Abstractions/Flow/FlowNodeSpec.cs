namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// One input or output node of a component kind, as the component catalogue states it (FDW-1395).
/// </summary>
/// <param name="Name">The node's name, or for a node named by the person building the graph, what the node is for.</param>
/// <param name="IsNamedByUser">Whether the person building the graph names each node of this kind; false when the kind fixes the name.</param>
/// <param name="Min">The fewest nodes of this kind a component of the kind has.</param>
/// <param name="Max">The most nodes of this kind a component of the kind has; <see langword="null"/> when there is no limit.</param>
/// <param name="IsError">Whether the output node takes the rows a component could not process; always false on an input.</param>
/// <param name="HelpText">What rows arrive at, or leave through, the node.</param>
/// <param name="NamedBySetting">
/// For a node named by the person building the graph: the setting of the component that gives the node its name
/// (<c>outputs</c> names a split's filtered outputs from their rows, <c>errorOutput</c> and <c>unmatchedOutput</c> name one
/// node each); <see langword="null"/> when the graph's links name it, and for a node whose name the kind fixes.
/// </param>
public sealed record FlowNodeSpec(
    string Name,
    bool IsNamedByUser,
    int Min,
    int? Max,
    bool IsError,
    string HelpText,
    string? NamedBySetting = null);
