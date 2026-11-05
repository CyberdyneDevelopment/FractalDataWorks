using System;
using System.Threading.Tasks;
using Fdw.Orchestration.Abstractions.Resilience;
using Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Fdw.Orchestration.Abstractions.TypeCollections;

namespace Fdw.Orchestration.Resilience;

/// <summary>
/// Factory for creating Polly resilience pipelines from orchestration configuration.
/// </summary>
/// <remarks>
/// Bridges our TypeCollection-based configuration (WHAT strategy to use) with
/// Polly's resilience pipeline execution (HOW to apply it).
/// </remarks>
public sealed class PollyResiliencePipelineFactory : IResiliencePipelineFactory
{
    /// <inheritdoc/>
    public ResiliencePipeline Create(ResilienceOptions options)
    {
        var builder = new ResiliencePipelineBuilder();

        ConfigureRetry(builder, options);
        ConfigureCircuitBreaker(builder, options);
        ConfigureTimeout(builder, options);

        return builder.Build();
    }

    /// <inheritdoc/>
    public ResiliencePipeline<TResult> Create<TResult>(ResilienceOptions options)
    {
        var builder = new ResiliencePipelineBuilder<TResult>();

        ConfigureRetry(builder, options);
        ConfigureCircuitBreaker(builder, options);
        ConfigureTimeout(builder, options);

        return builder.Build();
    }

    private static void ConfigureRetry(ResiliencePipelineBuilder builder, ResilienceOptions options)
    {
        if (options.MaxRetryAttempts <= 0)
        {
            return;
        }

        var retryOptions = new RetryStrategyOptions
        {
            MaxRetryAttempts = options.MaxRetryAttempts,
            DelayGenerator = CreateDelayGenerator(options.BackoffStrategy),
            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                options.ShouldRetryOnException is not { } shouldRetry || shouldRetry(ex)),
            OnRetry = args =>
            {
                options.OnRetry?.Invoke(
                    args.Outcome.Exception!,
                    args.AttemptNumber,
                    args.RetryDelay);
                return default;
            }
        };

        builder.AddRetry(retryOptions);
    }

    private static void ConfigureRetry<TResult>(ResiliencePipelineBuilder<TResult> builder, ResilienceOptions options)
    {
        if (options.MaxRetryAttempts <= 0)
        {
            return;
        }

        var retryOptions = new RetryStrategyOptions<TResult>
        {
            MaxRetryAttempts = options.MaxRetryAttempts,
            DelayGenerator = CreateDelayGenerator<TResult>(options.BackoffStrategy),
            ShouldHandle = new PredicateBuilder<TResult>().Handle<Exception>(ex =>
                options.ShouldRetryOnException is not { } shouldRetry || shouldRetry(ex)),
            OnRetry = args =>
            {
                if (args.Outcome.Exception != null)
                {
                    options.OnRetry?.Invoke(
                        args.Outcome.Exception,
                        args.AttemptNumber,
                        args.RetryDelay);
                }
                return default;
            }
        };

        builder.AddRetry(retryOptions);
    }

    private static void ConfigureCircuitBreaker(ResiliencePipelineBuilder builder, ResilienceOptions options)
    {
        if (options.CircuitBreaker is not { } breaker)
        {
            return;
        }

        var circuitBreakerOptions = new CircuitBreakerStrategyOptions
        {
            FailureRatio = breaker.FailureRatio,
            MinimumThroughput = breaker.MinimumThroughput,
            BreakDuration = breaker.BreakDuration,
            SamplingDuration = breaker.SamplingDuration
        };

        builder.AddCircuitBreaker(circuitBreakerOptions);
    }

    private static void ConfigureCircuitBreaker<TResult>(ResiliencePipelineBuilder<TResult> builder, ResilienceOptions options)
    {
        if (options.CircuitBreaker is not { } breaker)
        {
            return;
        }

        var circuitBreakerOptions = new CircuitBreakerStrategyOptions<TResult>
        {
            FailureRatio = breaker.FailureRatio,
            MinimumThroughput = breaker.MinimumThroughput,
            BreakDuration = breaker.BreakDuration,
            SamplingDuration = breaker.SamplingDuration
        };

        builder.AddCircuitBreaker(circuitBreakerOptions);
    }

    private static void ConfigureTimeout(ResiliencePipelineBuilder builder, ResilienceOptions options)
    {
        if (!options.Timeout.HasValue)
        {
            return;
        }

        var timeoutOptions = new TimeoutStrategyOptions
        {
            Timeout = options.Timeout.Value
        };

        builder.AddTimeout(timeoutOptions);
    }

    private static void ConfigureTimeout<TResult>(ResiliencePipelineBuilder<TResult> builder, ResilienceOptions options)
    {
        if (!options.Timeout.HasValue)
        {
            return;
        }

        var timeoutOptions = new TimeoutStrategyOptions
        {
            Timeout = options.Timeout.Value
        };

        builder.AddTimeout(timeoutOptions);
    }

    private static Func<RetryDelayGeneratorArguments<object>, ValueTask<TimeSpan?>> CreateDelayGenerator(
        IBackoffStrategy? backoffStrategy)
    {
        return args =>
        {
            if (backoffStrategy == null)
            {
                // Default to 1 second fixed delay
                return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(1));
            }

            var delay = backoffStrategy.GetDelay(args.AttemptNumber);
            return new ValueTask<TimeSpan?>(delay);
        };
    }

    private static Func<RetryDelayGeneratorArguments<TResult>, ValueTask<TimeSpan?>> CreateDelayGenerator<TResult>(
        IBackoffStrategy? backoffStrategy)
    {
        return args =>
        {
            if (backoffStrategy == null)
            {
                // Default to 1 second fixed delay
                return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(1));
            }

            var delay = backoffStrategy.GetDelay(args.AttemptNumber);
            return new ValueTask<TimeSpan?>(delay);
        };
    }
}
