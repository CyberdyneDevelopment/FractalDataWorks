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
            var pollyResult = await pipeline.ExecuteAsync(
                async (ct) =>
                {
                    var stageResult = await runStage(ct).ConfigureAwait(false);
                    if (!stageResult.IsSuccess)
                    {
                        lastFailureMessage = stageResult.CurrentMessage;
                        throw new ResiliencyRetryException(lastFailureMessage ?? "Stage failed");
                    }

                    return stageResult;
                }, cancellationToken)
            .ConfigureAwait(false);

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
            throw;
        }
        catch (ResiliencyRetryException ex)
        {
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

        if (config.TimeoutSeconds.HasValue)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(config.TimeoutSeconds.Value));
        }

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
        return DelayBackoffType.Exponential;
    }
}
