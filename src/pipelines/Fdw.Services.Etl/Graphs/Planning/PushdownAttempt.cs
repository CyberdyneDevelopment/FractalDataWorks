using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// The planner's answer for one component (FDW-6969): the side it reads once pushed, or the reason it runs in memory. A part at the top of a pushed
/// sub-graph may also add a field that says whether one side of it is present, and what happens to a row without it.
/// </summary>
/// <param name="Side">The pushed side; null when the component runs in memory.</param>
/// <param name="Reason">Why the component runs in memory; empty when it is pushed.</param>
/// <param name="Marker">The field the query adds to say whether a side is present; null when there is none.</param>
/// <param name="Absence">What happens to a row whose side is absent; null when there is none.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record PushdownAttempt(PushdownSide? Side, string Reason, PushdownColumn? Marker, FlowPushedAbsence? Absence)
{
    /// <summary>Gets how many rows a pushed query sends in a batch, as the top component states it; zero until a part states it, and refused below one.</summary>
    public int BatchSize { get; init; }

    /// <summary>Makes the answer of a component that is pushed.</summary>
    /// <param name="side">What it reads.</param>
    /// <returns>The answer.</returns>
    public static PushdownAttempt Pushed(PushdownSide side) => new(side, string.Empty, null, null);

    /// <summary>Makes the answer of a component that runs in memory.</summary>
    /// <param name="reason">Why.</param>
    /// <returns>The answer.</returns>
    public static PushdownAttempt Stays(string reason) => new(null, reason, null, null);
}
