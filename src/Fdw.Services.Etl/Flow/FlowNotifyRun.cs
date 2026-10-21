using System;

namespace Fdw.Services.Etl.Flow;

/// <summary>What a notification says about the run it belongs to: which pipeline, which execution, and when it began.</summary>
/// <param name="PipelineId">The pipeline's durable id.</param>
/// <param name="PipelineName">The pipeline's name.</param>
/// <param name="ExecutionId">The execution the run is.</param>
/// <param name="StartedAt">When the run started.</param>
public sealed record FlowNotifyRun(Guid PipelineId, string PipelineName, Guid ExecutionId, DateTimeOffset StartedAt);
