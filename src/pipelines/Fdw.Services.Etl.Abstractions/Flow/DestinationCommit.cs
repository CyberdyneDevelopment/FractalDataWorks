namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What a load destination has committed to its target so far (FDW-3785), reported to the run's records so a person can see, after the run was stopped or lost,
/// how many rows reached the target and whether the target was emptied first. A load that is run again reads this to refuse loading the same rows twice.
/// </summary>
/// <param name="Destination">The destination component's name.</param>
/// <param name="RowsCommitted">The rows the load's commands have affected and committed since the run started, over every batch so far.</param>
/// <param name="TargetEmptied">Whether the target was emptied for this run (a Replace load); the rows the target held before are gone.</param>
/// <param name="WholeSet">Whether the load holds every row until its input ends and writes them once (a soft delete or a file write): nothing is committed until the end.</param>
/// <param name="RepeatsToSameResult">Whether running the load again over the same rows leaves the target as it is now: true for a load that empties its target first and for a kind that says so (FDW-3840).</param>
public sealed record DestinationCommit(string Destination, long RowsCommitted, bool TargetEmptied, bool WholeSet, bool RepeatsToSameResult)
{
    /// <summary>Gets how many keys of the destination's updates or deletes matched no row so far under the Warn rule (FDW-4988); 0 for a destination that matches none by key.</summary>
    public long UnmatchedKeys { get; init; }

    /// <summary>Gets up to the run's sample size of those keys, or <see langword="null"/> when none matched no row.</summary>
    public System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyDictionary<string, object?>>? UnmatchedSample { get; init; }
}
