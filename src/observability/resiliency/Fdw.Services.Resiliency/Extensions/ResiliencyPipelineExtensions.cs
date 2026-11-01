using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Fdw.Results;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Logging;

namespace Fdw.Services.Resiliency.Extensions;

/// <summary>
/// Extension methods for creating Polly ResiliencePipeline instances from IResiliencyPolicy configurations.
/// </summary>
public static class ResiliencyPipelineExtensions
{
    /// <summary>
    /// Creates a Polly ResiliencePipeline from the resiliency policy configuration.
    /// </summary>
    /// <param name="policy">The resiliency policy configuration.</param>
    /// <param name="logger">Optional logger for pipeline events.</param>
    /// <param name="operationName">Optional operation name for logging context. Defaults to policy name.</param>
    /// <returns>A configured ResiliencePipeline instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static ResiliencePipeline Create(
        this IResiliencyPolicy policy,
        ILogger? logger = null,
        string? operationName = null)
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        operationName ??= policy.Name;

        if (logger != null)
        {
            ResiliencyLog.CreatingPipeline(logger, policy.Name);
        }

        var builder = new ResiliencePipelineBuilder()
            .AddRetry(CreateRetryOptions(policy, logger, operationName))
            .AddCircuitBreaker(CreateCircuitBreakerOptions(policy, logger, operationName))
            .AddTimeout(CreateTimeoutOptions(policy, logger, operationName));

        var pipeline = builder.Build();

        if (logger != null)
        {
            ResiliencyLog.PolicyRegistered(logger, policy.Name, policy.MaxRetries);
        }

        return pipeline;
    }

    /// <summary>
    /// Creates a generic Polly ResiliencePipeline{TResult} from the resiliency policy configuration.
    /// </summary>
    /// <typeparam name="TResult">The result type for the pipeline.</typeparam>
    /// <param name="policy">The resiliency policy configuration.</param>
    /// <param name="logger">Optional logger for pipeline events.</param>
    /// <param name="operationName">Optional operation name for logging context. Defaults to policy name.</param>
    /// <returns>A configured ResiliencePipeline{TResult} instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static ResiliencePipeline<TResult> CreatePipeline<TResult>(
        this IResiliencyPolicy policy,
        ILogger? logger = null,
        string? operationName = null)
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        operationName ??= policy.Name;

        if (logger != null)
        {
            ResiliencyLog.CreatingPipeline(logger, policy.Name);
        }

        var builder = new ResiliencePipelineBuilder<TResult>()
            .AddRetry(CreateGenericRetryOptions<TResult>(policy, logger, operationName))
            .AddCircuitBreaker(CreateGenericCircuitBreakerOptions<TResult>(policy, logger, operationName))
            .AddTimeout(CreateTimeoutOptions(policy, logger, operationName));

        var pipeline = builder.Build();

        if (logger != null)
        {
            ResiliencyLog.PolicyRegistered(logger, policy.Name, policy.MaxRetries);
        }

        return pipeline;
    }

    /// <summary>
    /// Executes an operation with resiliency using the policy configuration.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="policy">The resiliency policy configuration.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="logger">Optional logger for pipeline events.</param>
    /// <param name="operationName">Optional operation name for logging context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the operation output or error information.</returns>
    public static async Task<IGenericResult<TResult>> Execute<TResult>(
        this IResiliencyPolicy policy,
        Func<CancellationToken, Task<TResult>> operation,
        ILogger? logger = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        operationName ??= policy.Name;

        try
        {
            var pipeline = policy.CreatePipeline<TResult>(logger, operationName);
            var result = await pipeline.ExecuteAsync(
                async ct => await operation(ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            if (logger != null)
            {
                ResiliencyLog.OperationSucceededAfterRetry(logger, operationName, 1);
            }

            return GenericResult<TResult>.Success(result);
        }
        catch (TimeoutRejectedException ex)
        {
            var effectiveLogger = logger ?? NullLogger.Instance;
            ResiliencyLog.OperationTimedOut(effectiveLogger, operationName, 30);
            return GenericResult<TResult>.Failure(
                ResiliencyLog.PipelineExecutionException(effectiveLogger, ex, operationName));
        }
        catch (BrokenCircuitException ex)
        {
            var effectiveLogger = logger ?? NullLogger.Instance;
            ResiliencyLog.CircuitBreakerRejected(effectiveLogger, operationName);
            return GenericResult<TResult>.Failure(
                ResiliencyLog.PipelineExecutionException(effectiveLogger, ex, operationName));
        }
        catch (Exception ex)
        {
            var effectiveLogger = logger ?? NullLogger.Instance;
            return GenericResult<TResult>.Failure(
                ResiliencyLog.PipelineExecutionException(effectiveLogger, ex, operationName));
        }
    }

    /// <summary>
    /// Executes an operation with resiliency using the policy configuration.
    /// </summary>
    /// <param name="policy">The resiliency policy configuration.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="logger">Optional logger for pipeline events.</param>
    /// <param name="operationName">Optional operation name for logging context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or error information.</returns>
    public static async Task<IGenericResult> Execute(
        this IResiliencyPolicy policy,
        Func<CancellationToken, Task> operation,
        ILogger? logger = null,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        operationName ??= policy.Name;
        var effectiveLogger = logger ?? NullLogger.Instance;

        try
        {
            var pipeline = policy.Create(logger, operationName);
            await pipeline.ExecuteAsync(
                async ct =>
                {
                    await operation(ct).ConfigureAwait(false);
                },
                cancellationToken).ConfigureAwait(false);

            ResiliencyLog.OperationSucceededAfterRetry(effectiveLogger, operationName, 1);
            return GenericResult.Success();
        }
        catch (TimeoutRejectedException ex)
        {
            ResiliencyLog.OperationTimedOut(effectiveLogger, operationName, 30);
            return GenericResult.Failure(
                ResiliencyLog.PipelineExecutionException(effectiveLogger, ex, operationName));
        }
        catch (BrokenCircuitException ex)
        {
            ResiliencyLog.CircuitBreakerRejected(effectiveLogger, operationName);
            return GenericResult.Failure(
                ResiliencyLog.PipelineExecutionException(effectiveLogger, ex, operationName));
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                ResiliencyLog.PipelineExecutionException(effectiveLogger, ex, operationName));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Private Helper Methods
    // ═══════════════════════════════════════════════════════════════════════════

    private static RetryStrategyOptions CreateRetryOptions(
        IResiliencyPolicy policy,
        ILogger? logger,
        string operationName)
    {
        return new RetryStrategyOptions
        {
            MaxRetryAttempts = policy.MaxRetries,
            Delay = policy.InitialDelay,
            MaxDelay = policy.MaxDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = CreateRetryCallback(logger, operationName, policy.MaxRetries)
        };
    }

    private static RetryStrategyOptions<TResult> CreateGenericRetryOptions<TResult>(
        IResiliencyPolicy policy,
        ILogger? logger,
        string operationName)
    {
        return new RetryStrategyOptions<TResult>
        {
            MaxRetryAttempts = policy.MaxRetries,
            Delay = policy.InitialDelay,
            MaxDelay = policy.MaxDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = CreateGenericRetryCallback<TResult>(logger, operationName, policy.MaxRetries)
        };
    }

    private static CircuitBreakerStrategyOptions CreateCircuitBreakerOptions(
        IResiliencyPolicy policy,
        ILogger? logger,
        string operationName)
    {
        return new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = policy.CircuitBreakerThreshold,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = policy.CircuitBreakerDuration,
            OnOpened = CreateCircuitOpenedCallback(logger, operationName, policy.CircuitBreakerDuration),
            OnClosed = CreateCircuitClosedCallback(logger, operationName),
            OnHalfOpened = CreateCircuitHalfOpenedCallback(logger, operationName)
        };
    }

    private static CircuitBreakerStrategyOptions<TResult> CreateGenericCircuitBreakerOptions<TResult>(
        IResiliencyPolicy policy,
        ILogger? logger,
        string operationName)
    {
        return new CircuitBreakerStrategyOptions<TResult>
        {
            FailureRatio = 0.5,
            MinimumThroughput = policy.CircuitBreakerThreshold,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = policy.CircuitBreakerDuration,
            OnOpened = CreateGenericCircuitOpenedCallback<TResult>(logger, operationName, policy.CircuitBreakerDuration),
            OnClosed = CreateGenericCircuitClosedCallback<TResult>(logger, operationName),
            OnHalfOpened = CreateGenericCircuitHalfOpenedCallback<TResult>(logger, operationName)
        };
    }

    private static TimeoutStrategyOptions CreateTimeoutOptions(
        IResiliencyPolicy policy,
        ILogger? logger,
        string operationName)
    {
        // Use a reasonable default timeout based on the policy's max delay and retries
        var timeout = TimeSpan.FromSeconds(Math.Max(30, policy.MaxDelay.TotalSeconds * (policy.MaxRetries + 1)));

        if (logger != null)
        {
            ResiliencyLog.TimeoutConfigured(logger, (int)timeout.TotalSeconds, operationName);
        }

        return new TimeoutStrategyOptions
        {
            Timeout = timeout,
            OnTimeout = CreateTimeoutCallback(logger, operationName)
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Callback Factory Methods
    // ═══════════════════════════════════════════════════════════════════════════

    [ExcludeFromCodeCoverage(Justification = "Retry callback - tested via integration tests")]
    private static Func<OnRetryArguments<object>, ValueTask> CreateRetryCallback(
        ILogger? logger,
        string operationName,
        int maxRetries)
    {
        return args =>
        {
            if (logger != null)
            {
                ResiliencyLog.RetryAttempt(logger, args.AttemptNumber, maxRetries, operationName);
                ResiliencyLog.RetryDelayApplied(logger, args.RetryDelay.TotalMilliseconds, operationName);
            }
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Retry callback - tested via integration tests")]
    private static Func<OnRetryArguments<TResult>, ValueTask> CreateGenericRetryCallback<TResult>(
        ILogger? logger,
        string operationName,
        int maxRetries)
    {
        return args =>
        {
            if (logger != null)
            {
                ResiliencyLog.RetryAttempt(logger, args.AttemptNumber, maxRetries, operationName);
                ResiliencyLog.RetryDelayApplied(logger, args.RetryDelay.TotalMilliseconds, operationName);
            }
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private static Func<OnCircuitOpenedArguments<object>, ValueTask> CreateCircuitOpenedCallback(
        ILogger? logger,
        string operationName,
        TimeSpan duration)
    {
        return _ =>
        {
            if (logger != null)
            {
                ResiliencyLog.CircuitBreakerOpened(logger, operationName, (int)duration.TotalSeconds);
            }
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private static Func<OnCircuitOpenedArguments<TResult>, ValueTask> CreateGenericCircuitOpenedCallback<TResult>(
        ILogger? logger,
        string operationName,
        TimeSpan duration)
    {
        return _ =>
        {
            if (logger != null)
            {
                ResiliencyLog.CircuitBreakerOpened(logger, operationName, (int)duration.TotalSeconds);
            }
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private static Func<OnCircuitClosedArguments<object>, ValueTask> CreateCircuitClosedCallback(
        ILogger? logger,
        string operationName)
    {
        return _ =>
        {
            if (logger != null)
            {
                ResiliencyLog.CircuitBreakerClosed(logger, operationName);
            }
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private static Func<OnCircuitClosedArguments<TResult>, ValueTask> CreateGenericCircuitClosedCallback<TResult>(
        ILogger? logger,
        string operationName)
    {
        return _ =>
        {
            if (logger != null)
            {
                ResiliencyLog.CircuitBreakerClosed(logger, operationName);
            }
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private static Func<OnCircuitHalfOpenedArguments, ValueTask> CreateCircuitHalfOpenedCallback(
        ILogger? logger,
        string operationName)
    {
        return _ =>
        {
            if (logger != null)
            {
                ResiliencyLog.CircuitBreakerHalfOpen(logger, operationName);
            }
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private static Func<OnCircuitHalfOpenedArguments, ValueTask> CreateGenericCircuitHalfOpenedCallback<TResult>(
        ILogger? logger,
        string operationName)
    {
        return _ =>
        {
            if (logger != null)
            {
                ResiliencyLog.CircuitBreakerHalfOpen(logger, operationName);
            }
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Timeout callback - tested via integration tests")]
    private static Func<OnTimeoutArguments, ValueTask> CreateTimeoutCallback(
        ILogger? logger,
        string operationName)
    {
        return args =>
        {
            if (logger != null)
            {
                ResiliencyLog.OperationTimedOut(logger, operationName, (int)args.Timeout.TotalSeconds);
            }
            return ValueTask.CompletedTask;
        };
    }
}
