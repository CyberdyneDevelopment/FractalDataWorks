using System;
using Fdw.Services.Resiliency.Abstractions;

namespace Fdw.Services.Resiliency;

/// <summary>
/// Mutable execution context passed into each strategy invocation.
/// Created fresh per stage execution; <see cref="AttemptNumber"/> is incremented
/// by the executor on each retry.
/// </summary>
public sealed class ResiliencyExecutionContext : IResiliencyExecutionContext
{
    /// <inheritdoc/>
    public Guid ExecutionId { get; set; }

    /// <inheritdoc/>
    public Guid StageId { get; set; }

    /// <inheritdoc/>
    public Guid? SourceDataSetId { get; set; }

    /// <inheritdoc/>
    public int AttemptNumber { get; set; }
}
