using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The scheduler's list at one instant (FDW-3097): the runs in flight, and the runs waiting in the order they arrived.
/// </summary>
/// <param name="Running">The runs in flight.</param>
/// <param name="Waiting">The runs waiting for their turn, in arrival order, each with the reason it cannot start now.</param>
public sealed record PipelineRunQueueSnapshot(IReadOnlyList<PipelineRunQueueEntry> Running, IReadOnlyList<PipelineRunQueueEntry> Waiting);
