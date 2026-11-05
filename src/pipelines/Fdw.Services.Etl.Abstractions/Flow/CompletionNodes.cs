using System;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// The two nodes every component kind has besides its rows (FDW-3640): an input <c>after</c> and an output <c>done</c>. A line from a <c>done</c> to an
/// <c>after</c> carries no rows: it says the component at its end does not start until the component at its start has finished.
/// </summary>
/// <remarks>
/// Why they are stated here and not in the nodes a kind declares: what a kind is follows from its row nodes (no outputs makes a destination, any input gives it
/// mappings), and a kind is not a destination or a source of rows because it also has these. A line is a completion line when it leaves a <c>done</c>; the inputs it
/// arrives at are named by the person, one for each line, because an input takes one line.
/// </remarks>
public static class CompletionNodes
{
    /// <summary>The name of the output every component has: its completion.</summary>
    public const string Done = "done";

    /// <summary>The name a person gives the first completion input of a component; further ones are named by the person like a union's inputs.</summary>
    public const string After = "after";

    /// <summary>Gets the input node every component has, for the catalogue: any number, each named by the person, each fed by one <c>done</c>.</summary>
    public static FlowNodeSpec AfterInput { get; } = new(
        After, true, 0, null, false, "A component that must have completed before this one starts: the line comes from its done output.", null, true);

    /// <summary>Gets the output node every component has, for the catalogue: it may be joined to the after input of any number of components.</summary>
    public static FlowNodeSpec DoneOutput { get; } = new(
        Done, false, 1, 1, false, "Joined to the after input of a component that must wait until this one has completed; no rows pass.", null, true);

    /// <summary>Says whether an output node name is the completion output.</summary>
    /// <param name="output">The output node name of a line.</param>
    /// <returns><see langword="true"/> when a line from it is a completion line.</returns>
    public static bool IsDone(string output) => string.Equals(output, Done, StringComparison.OrdinalIgnoreCase);
}
