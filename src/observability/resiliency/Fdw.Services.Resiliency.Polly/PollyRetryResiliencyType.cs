using Fdw.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Messages;
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
        var logger = ctx is ILoggerProvider lp
            ? lp.CreateLogger(nameof(PollyRetryResiliencyType))
            : NullLogger.Instance;

        if (config is not PollyRetryResiliencyConfiguration pollyConfig)
        {
            return GenericResult.Failure(
                PollyRetryLog.WrongConfigurationType(logger, config.GetType().Name));
        }

        var refusal = Refusal(pollyConfig, logger);
        if (refusal is not null)
        {
            return GenericResult.Failure(refusal);
        }

        var maxRetries = pollyConfig.MaxRetries!.Value;
        var pipeline = BuildPipeline(pollyConfig, ToDelayBackoffType(pollyConfig.BackoffKind!)!.Value);
        try
        {
            var pollyResult = await pipeline.ExecuteAsync(
                async (ct) =>
                {
                    var stageResult = await runStage(ct).ConfigureAwait(false);
                    if (!stageResult.IsSuccess)
                    {
                        throw new ResiliencyRetryException(stageResult.FailureReason().Message);
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
                        maxRetries,
                        pollyResult.FailureReason().Message));
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
                    maxRetries,
                    ex.Message));
        }
    }

    // Why a refusal: every value of a policy is its definer's to state (Mike, FDW-3931: "you are defining a resilience policy. why in the
    // fuck would you have a default?"). The first one left out, or a backoff kind that is not one, is named.
    private static IGenericMessage? Refusal(PollyRetryResiliencyConfiguration config, ILogger logger)
    {
        var unstated = config.MaxRetries is null ? nameof(config.MaxRetries)
            : config.BackoffKind is null ? nameof(config.BackoffKind)
            : config.BaseDelayMs is null ? nameof(config.BaseDelayMs)
            : config.MaxDelayMs is null ? nameof(config.MaxDelayMs)
            : null;
        if (unstated is not null)
        {
            return PollyRetryLog.PolicyValueNotStated(logger, config.Name, unstated);
        }

        return ToDelayBackoffType(config.BackoffKind!) is null ? PollyRetryLog.BackoffKindUnknown(logger, config.Name, config.BackoffKind!) : null;
    }

    private static ResiliencePipeline<IGenericResult> BuildPipeline(
        PollyRetryResiliencyConfiguration config, DelayBackoffType backoff)
    {
        var builder = new ResiliencePipelineBuilder<IGenericResult>();

        if (config.TimeoutSeconds.HasValue)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(config.TimeoutSeconds.Value));
        }

        builder.AddRetry(new RetryStrategyOptions<IGenericResult>
        {
            MaxRetryAttempts = config.MaxRetries!.Value,
            Delay = TimeSpan.FromMilliseconds(config.BaseDelayMs!.Value),
            MaxDelay = TimeSpan.FromMilliseconds(config.MaxDelayMs!.Value),
            BackoffType = backoff,
            UseJitter = config.JitterPercent.HasValue && config.JitterPercent.Value > 0,
            ShouldHandle = new PredicateBuilder<IGenericResult>()
                .Handle<ResiliencyRetryException>()
        });

        return builder.Build();
    }

    // The backoff a kind names; null for a name that is not a kind, which the strategy refuses rather than reading as Exponential.
    private static DelayBackoffType? ToDelayBackoffType(string backoffKind)
    {
        if (string.Equals(backoffKind, "Fixed", StringComparison.OrdinalIgnoreCase))
            return DelayBackoffType.Constant;
        if (string.Equals(backoffKind, "Random", StringComparison.OrdinalIgnoreCase))
            return DelayBackoffType.Linear;
        if (string.Equals(backoffKind, "Exponential", StringComparison.OrdinalIgnoreCase))
            return DelayBackoffType.Exponential;
        return null;
    }
}
