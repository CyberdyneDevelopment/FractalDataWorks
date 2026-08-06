using Fdw.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Resiliency;
using Fdw.Services.Resiliency.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Fdw.Services.Resiliency.Polly;

/// <summary>
/// PollyRetry resiliency strategy. Wraps stage execution in a Polly ResiliencePipeline
/// configured from <see cref="PollyRetryResiliencyConfiguration"/>.
/// </summary>
/// <remarks>
/// Supports: exponential/fixed/random backoff, optional jitter, optional circuit-breaker,
/// optional per-attempt timeout. All retry intelligence is encapsulated here;
/// the orchestrator simply calls Execute and awaits the result.
/// </remarks>
[TypeOption(typeof(ResiliencyTypes), "PollyRetry")]
public sealed class PollyRetryResiliencyType : ResiliencyTypeBase
{
    /// <summary>Initializes a new instance of <see cref="PollyRetryResiliencyType"/>.</summary>
    public PollyRetryResiliencyType()
        : base(
            id: 2,
            name: "PollyRetry",
            displayName: "Polly Retry",
            description: "Retry with configurable backoff, optional circuit-breaker, and timeout.")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult> Execute(
        Func<CancellationToken, Task<IGenericResult>> runStage,
        IGenericConfiguration config,
        IResiliencyExecutionContext ctx,
        CancellationToken cancellationToken)
    {
        if (runStage == null) throw new ArgumentNullException(nameof(runStage));
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));

        // Why: TypeOptions are DI-free; logger is retrieved from context if available.
        var logger = ctx is ILoggerProvider lp
            ? lp.CreateLogger(nameof(PollyRetryResiliencyType))
            : NullLogger.Instance;

        if (config is not PollyRetryResiliencyConfiguration pollyConfig)
        {
            return GenericResult.Failure(
                PollyRetryLog.WrongConfigurationType(logger, config.GetType().Name));
        }

        var pipeline = BuildPipeline(pollyConfig);
        string? lastFailureMessage = null;

        try
        {
            // Why: Polly returns the result from the delegate on success.
            // On retry-exhaustion, it re-throws the last ResiliencyRetryException.
            var pollyResult = await pipeline.ExecuteAsync(
                async (ct) =>
                {
                    var stageResult = await runStage(ct).ConfigureAwait(false);
                    if (!stageResult.IsSuccess)
                    {
                        // Why: capture the failure message before throwing so the catch block
                        // has context without needing to reference the result across boundaries.
                        lastFailureMessage = stageResult.CurrentMessage;
                        throw new ResiliencyRetryException(lastFailureMessage ?? "Stage failed");
                    }

                    return stageResult;
                }, cancellationToken)
            .ConfigureAwait(false);

            // Why: inspect IsSuccess per FDW012 — all IGenericResult values must be checked.
            if (!pollyResult.IsSuccess)
            {
                return GenericResult.Failure(
                    PollyRetryLog.RetriesExhausted(
                        logger,
                        ctx.ExecutionId,
                        pollyConfig.MaxRetries,
                        pollyResult.CurrentMessage ?? "Polly returned failure"));
            }

            return pollyResult;
        }
        catch (OperationCanceledException)
        {
            // Why: propagate cancellation without wrapping.
            throw;
        }
        catch (ResiliencyRetryException ex)
        {
            // Why: all retries exhausted — return a failure result using the last captured message.
            // ex.Message is the fallback when lastFailureMessage was not captured (e.g. immediate throw path).
            return GenericResult.Failure(
                PollyRetryLog.RetriesExhausted(
                    logger,
                    ctx.ExecutionId,
                    pollyConfig.MaxRetries,
                    lastFailureMessage ?? ex.Message));
        }
    }

    private static ResiliencePipeline<IGenericResult> BuildPipeline(
        PollyRetryResiliencyConfiguration config)
    {
        var builder = new ResiliencePipelineBuilder<IGenericResult>();

        // Why: add timeout strategy first so it wraps each attempt.
        if (config.TimeoutSeconds.HasValue)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(config.TimeoutSeconds.Value));
        }

        // Why: retry strategy with configurable backoff.
        builder.AddRetry(new RetryStrategyOptions<IGenericResult>
        {
            MaxRetryAttempts = config.MaxRetries,
            Delay = TimeSpan.FromMilliseconds(config.BaseDelayMs),
            MaxDelay = TimeSpan.FromMilliseconds(config.MaxDelayMs),
            BackoffType = ToDelayBackoffType(config.BackoffKind),
            UseJitter = config.JitterPercent.HasValue && config.JitterPercent.Value > 0,
            ShouldHandle = new PredicateBuilder<IGenericResult>()
                .Handle<ResiliencyRetryException>()
        });

        return builder.Build();
    }

    private static DelayBackoffType ToDelayBackoffType(string backoffKind)
    {
        if (string.Equals(backoffKind, "Fixed", StringComparison.OrdinalIgnoreCase))
            return DelayBackoffType.Constant;
        if (string.Equals(backoffKind, "Random", StringComparison.OrdinalIgnoreCase))
            return DelayBackoffType.Linear;
        // Why: default to Exponential — most common and safe for transient failures.
        return DelayBackoffType.Exponential;
    }
}
