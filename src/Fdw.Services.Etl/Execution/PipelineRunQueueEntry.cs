using System;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// One run in the scheduler's list: in flight, or waiting for its turn (FDW-3097).
/// </summary>
/// <param name="Run">Which run it is.</param>
/// <param name="QueuedAt">When the scheduler accepted the run.</param>
/// <param name="StartedAt">When the run started, or <see langword="null"/> for a run that has not.</param>
/// <param name="Waiting">Why the run cannot start now, or <see langword="null"/> for a run in flight.</param>
public sealed record PipelineRunQueueEntry(PipelineRunIdentity Run, DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, PipelineRunBlock? Waiting);
