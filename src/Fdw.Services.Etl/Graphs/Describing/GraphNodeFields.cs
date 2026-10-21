using System.Collections.Generic;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// The fields at one input or output node of a component (FDW-1397).
/// </summary>
/// <param name="Component">The component's name.</param>
/// <param name="Node">The node's name.</param>
/// <param name="IsInput">Whether the node is an input; an output when false.</param>
/// <param name="Fields">The fields a row has there; <see langword="null"/> when they cannot be known before a run.</param>
/// <param name="Seen">For an input, the fields the component sees there once the mappings on that input are applied (FDW-2183): the mapped fields for a mapped input,
/// <paramref name="Fields"/> otherwise; <see langword="null"/> when they cannot be known, and for an output.</param>
/// <remarks>
/// <paramref name="Fields"/> stays what arrives at an input, because it is what the person building a mapping picks from; <paramref name="Seen"/> is what the
/// component works with, so a destination, which has no output, still states the fields it receives under the names it receives them as.
/// </remarks>
public sealed record GraphNodeFields(string Component, string Node, bool IsInput, IReadOnlyList<GraphField>? Fields, IReadOnlyList<GraphField>? Seen);
