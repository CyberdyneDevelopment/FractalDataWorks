using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Base execution context shared by every execution-scope domain context.
/// </summary>
/// <remarks>
/// All per-run state that every domain context previously redeclared independently
/// is consolidated here. Domain contexts inherit this interface and add only
/// domain-specific members.
/// </remarks>
// Why: Eliminates the cross-cutting duplication of ExecutionId/StartTime/CancellationToken/Logger/
// Services/Parameters/SharedState that existed independently on every domain execution context.
// Composing rather than flattening keeps each domain context's surface area minimal.
public interface IExecutionContext
{
    /// <summary>
    /// Gets the unique identifier for this execution run.
    /// </summary>
    Guid ExecutionId { get; }

    /// <summary>
    /// Gets the time when execution started (UTC).
    /// </summary>
    DateTimeOffset StartTime { get; }

    /// <summary>
    /// Gets the cancellation token for this execution.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the logger scoped to this execution.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the service provider for resolving dependencies during execution.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets the read-only input parameters for this execution.
    /// Parameters are supplied by the caller and do not change during execution.
    /// </summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Gets the mutable shared-state dictionary for passing data between steps/stages.
    /// </summary>
    IDictionary<string, object?> SharedState { get; }
}
