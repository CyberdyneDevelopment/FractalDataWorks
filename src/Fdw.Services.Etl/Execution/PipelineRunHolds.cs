using System;
using System.Collections.Concurrent;
using Fdw.Services.Etl.Abstractions.Execution;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The in-memory <see cref="IPipelineRunHolds"/>: the requests of the runs that wait for approval, by execution id (FDW-1937).
/// </summary>
public sealed class PipelineRunHolds : IPipelineRunHolds
{
    private readonly ConcurrentDictionary<Guid, PipelineExecutionRequest> _held = new();

    /// <inheritdoc/>
    public bool Hold(PipelineExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _held.TryAdd(request.ExecutionId, request);
    }

    /// <inheritdoc/>
    public PipelineExecutionRequest? Take(Guid executionId) => _held.TryRemove(executionId, out var request) ? request : null;
}
