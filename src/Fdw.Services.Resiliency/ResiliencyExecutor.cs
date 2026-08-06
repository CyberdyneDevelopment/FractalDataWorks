using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Resiliency;

/// <summary>
/// Default implementation of <see cref="IResiliencyExecutor"/>.
/// Resolves the policy configuration via <see cref="IResiliencyPolicyProvider"/>,
/// dispatches to the registered <see cref="IResiliencyType"/> via <see cref="ResiliencyTypes"/>,
/// and wraps the stage delegate.
/// </summary>
public sealed class ResiliencyExecutor : IResiliencyExecutor
{
    private readonly IResiliencyPolicyProvider _policyProvider;
    private readonly ILogger<ResiliencyExecutor> _logger;

    /// <summary>Initializes a new instance of <see cref="ResiliencyExecutor"/>.</summary>
    public ResiliencyExecutor(
        IResiliencyPolicyProvider policyProvider,
        ILogger<ResiliencyExecutor>? logger = null)
    {
        _policyProvider = policyProvider ?? throw new ArgumentNullException(nameof(policyProvider));
        // Why NullLogger fallback: per FDW convention, ensures the executor remains functional
        // if DI does not wire up logging.
        _logger = logger ?? NullLogger<ResiliencyExecutor>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Execute(
        Guid? policyId,
        Func<CancellationToken, Task<IGenericResult>> runStage,
        IResiliencyExecutionContext ctx,
        CancellationToken cancellationToken)
    {
        if (runStage == null) throw new ArgumentNullException(nameof(runStage));
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        ResiliencyLog.ResiliencyExecutorStarted(_logger, ctx.ExecutionId, ctx.StageId, policyId);

        // Why: null policyId means no resiliency configured — run once and return directly.
        if (policyId is null)
        {
            ResiliencyLog.PolicyNotFound(_logger, ctx.ExecutionId, "(none — pass-through)");
            return await runStage(cancellationToken).ConfigureAwait(false);
        }

        var configResult = await _policyProvider.Get(policyId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (!configResult.IsSuccess || configResult.Value is null)
        {
            // Why: policy not found is a configuration error — log and fail fast rather
            // than silently falling back to no-resiliency, which could mask misconfiguration.
            return GenericResult.Failure(
                ResiliencyLog.PolicyNotFound(_logger, ctx.ExecutionId, policyId.Value.ToString("N")));
        }

        var config = configResult.Value as ResiliencyConfiguration;
        if (config is null) { return GenericResult.Failure(ResiliencyLog.PolicyNotFound(_logger, ctx.ExecutionId, policyId.Value.ToString("N"))); }
        var strategyType = ResiliencyTypes.ByName(config.StrategyType);

        if (strategyType == ResiliencyTypes.NotFound)
        {
            return GenericResult.Failure(ResiliencyLog.StrategyNotFound(
                _logger, ctx.ExecutionId, config.StrategyType));
        }

        ResiliencyLog.PolicyResolved(_logger, ctx.ExecutionId, policyId.Value, config.StrategyType);
        ResiliencyLog.StrategyDispatched(_logger, ctx.ExecutionId, config.StrategyType);

        try
        {
            var result = await strategyType.Execute(runStage, config, ctx, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess)
                ResiliencyLog.AttemptSucceeded(_logger, ctx.ExecutionId, ctx.AttemptNumber);
            else
                ResiliencyLog.AttemptFailed(_logger, ctx.ExecutionId, ctx.AttemptNumber, result.CurrentMessage ?? "unknown");

            return result;
        }
        catch (OperationCanceledException)
        {
            // Why: cancellation is not an error — let it propagate silently.
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                ResiliencyLog.ResiliencyExecutorException(_logger, ex, ctx.ExecutionId, ctx.StageId));
        }
    }
}
