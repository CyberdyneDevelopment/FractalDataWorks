using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Tracking;

/// <summary>
/// What a run did (FDW-3381): its totals, and what each stage and each component did. A figure the run's records do not hold is null: a run from before the records
/// carried a figure answers what its records hold and nothing is invented.
/// </summary>
/// <param name="RunId">The run's execution record.</param>
/// <param name="Name">The record's name.</param>
/// <param name="State">The state of the run.</param>
/// <param name="StartedAt">When the run started.</param>
/// <param name="CompletedAt">When the run ended; null while it runs.</param>
/// <param name="RunTimeMs">How long the run ran, in milliseconds, from its record; null while it runs.</param>
/// <param name="RowsRead">The rows its sources sent on, over every stage.</param>
/// <param name="RowsWritten">The rows that arrived at its destinations.</param>
/// <param name="RowsRejected">The rows its error outputs carried.</param>
/// <param name="BytesMoved">The estimated size of the rows that moved over its lines: an estimate of the memory their values hold, not a size on disk or on the wire.</param>
/// <param name="Stages">The stages that moved rows, in the order they started.</param>
public sealed record RunTracking(
    Guid RunId, string Name, string State, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, long? RunTimeMs,
    long? RowsRead, long? RowsWritten, long? RowsRejected, long? BytesMoved, IReadOnlyList<StageTracking> Stages);
