using System;

namespace Fdw.Services.Pipelines.Abstractions.Runs;

/// <summary>Where a run is followed live and the state it is in now (FDW-6131).</summary>
/// <param name="ExecutionId">The run's execution.</param>
/// <param name="LiveGroup">The pipeline status hub group that carries the run's events (<c>execution:{id}</c>).</param>
/// <param name="State">The run's state on its execution record.</param>
public sealed record PipelineRunFollow(Guid ExecutionId, string LiveGroup, string State);
