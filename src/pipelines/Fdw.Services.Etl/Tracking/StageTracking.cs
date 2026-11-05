using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Tracking;

/// <summary>
/// What one stage of a run did (FDW-3381). A stage is a number, never a name: 0 is the components with no stage.
/// </summary>
/// <param name="Id">The stage's execution record.</param>
/// <param name="Name">The record's name: "Stage 2", or "No stage".</param>
/// <param name="Number">The stage number, or null for a record whose name does not state one.</param>
/// <param name="State">The state of the record.</param>
/// <param name="StartedAt">When the stage started: when its first component started; null when no component holds a start.</param>
/// <param name="CompletedAt">When the stage ended: when its last component ended; null when no component holds an end.</param>
/// <param name="RunTimeMs">How long the stage ran, in milliseconds, from its first component starting to its last one ending; null when either is missing. It is not read from the stage's record, which closes with the whole flow.</param>
/// <param name="RowsRead">The rows its sources sent on; null when the stage records no totals.</param>
/// <param name="RowsWritten">The rows that arrived at its destinations.</param>
/// <param name="RowsRejected">The rows its error outputs carried.</param>
/// <param name="BytesMoved">The estimated size of the rows that moved over the lines that leave its components.</param>
/// <param name="Components">What each of its components did, in the order they started.</param>
public sealed record StageTracking(
    Guid Id, string Name, int? Number, string State, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, long? RunTimeMs,
    long? RowsRead, long? RowsWritten, long? RowsRejected, long? BytesMoved, IReadOnlyList<ComponentTracking> Components);
