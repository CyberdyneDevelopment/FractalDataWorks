using System;

namespace Fdw.Services.Etl.Tracking;

/// <summary>
/// What one component did in a stage of a run (FDW-3381), as the stage's records state it. A figure the records do not hold is null, never zero.
/// </summary>
/// <param name="Name">The component's name.</param>
/// <param name="StartedAt">When it started.</param>
/// <param name="EndedAt">When it ended.</param>
/// <param name="RunTimeMs">How long it ran, in milliseconds.</param>
/// <param name="Succeeded">Whether it succeeded.</param>
/// <param name="Failed">Whether it was the component whose failure stopped the run.</param>
/// <param name="RowsIn">The rows that arrived at it.</param>
/// <param name="RowsOut">The rows it sent on.</param>
/// <param name="BytesOut">The estimated size of the rows it sent on.</param>
/// <param name="Rejected">The rows it sent out of its error outputs.</param>
/// <param name="RowsCommitted">For a load destination, the rows it had committed to its target when the run ended or was lost (FDW-3785); null for any other component and for a destination whose records hold no figure.</param>
/// <param name="TargetEmptied">For a load destination, whether it had emptied its target for the run (a Replace load); null for any other component and for a destination whose records hold no figure.</param>
/// <param name="RepeatsToSameResult">For a load destination, whether running its load again over the same rows leaves the target as it is (FDW-3840); null for any other component and for a destination whose records hold no figure.</param>
public sealed record ComponentTracking(
    string Name, DateTimeOffset? StartedAt, DateTimeOffset? EndedAt, long? RunTimeMs, bool? Succeeded, bool? Failed, long? RowsIn, long? RowsOut, long? BytesOut, long? Rejected,
    long? RowsCommitted, bool? TargetEmptied, bool? RepeatsToSameResult);
