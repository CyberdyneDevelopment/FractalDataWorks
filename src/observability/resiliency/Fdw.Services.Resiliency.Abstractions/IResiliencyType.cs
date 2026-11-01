using Fdw.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// TypeOption interface for pluggable resiliency strategies.
/// Each strategy (PollyRetry, PrimaryBackup, RetryNotify, None) registers as a TypeOption
/// against the ResiliencyTypes TypeCollection in its own assembly.
/// </summary>
/// <remarks>
/// The Execute method wraps the provided stage delegate, implementing retry/backoff/notify
/// behavior per the resolved configuration. All retry intelligence is encapsulated here —
/// the orchestrator simply calls Execute and waits for the result.
/// </remarks>
public interface IResiliencyType : ITypeOption<int, IResiliencyType>
{
    /// <summary>
    /// Executes the stage delegate with the resiliency strategy applied.
    /// </summary>
    /// <param name="runStage">The delegate that re-runs the entire stage from scratch.</param>
    /// <param name="config">The resolved strategy-specific configuration for this policy.</param>
    /// <param name="ctx">Execution context carrying ExecutionId, StageId, and attempt metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The aggregate result after applying the strategy (retries, backoff, fallback, etc.).</returns>
    Task<IGenericResult> Execute(
        Func<CancellationToken, Task<IGenericResult>> runStage,
        IGenericConfiguration config,
        IResiliencyExecutionContext ctx,
        CancellationToken cancellationToken);
}
