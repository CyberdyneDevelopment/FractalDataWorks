namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// One thing wrong with a flow graph (FDW-1396): where it is and why.
/// </summary>
/// <param name="Reason">What is wrong, in words written for the person building the graph.</param>
/// <param name="Component">The component it concerns, or <see langword="null"/> when it concerns the graph as a whole.</param>
/// <param name="Node">The input or output node it concerns, or <see langword="null"/> when it concerns the component or the graph.</param>
/// <param name="NodeKind">
/// <c>input</c> or <c>output</c> when <paramref name="Node"/> is stated; <c>link</c> when the problem is a link
/// (<paramref name="Component"/> is its producing component, <paramref name="Node"/> its output); otherwise <see langword="null"/>.
/// </param>
/// <param name="LinkIndex">The position in the graph's links of the link the problem concerns; <see langword="null"/> when it concerns no link.</param>
public sealed record FlowGraphProblem(
    string Reason, string? Component = null, string? Node = null, string? NodeKind = null, int? LinkIndex = null);
