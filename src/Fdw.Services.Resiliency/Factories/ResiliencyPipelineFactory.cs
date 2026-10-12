using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Fdw.Results;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Logging;

namespace Fdw.Services.Resiliency.Factories;

/// <summary>
/// Factory for creating and caching Polly ResiliencePipeline instances from policy configurations.
/// </summary>
/// <remarks>
/// <para>
/// This factory is designed to be a singleton service that efficiently manages ResiliencePipeline instances.
/// Pipelines are cached by policy name to avoid the overhead of recreating them for each request.
/// </para>
/// <para>
/// The factory uses thread-safe collections to ensure safe concurrent access from multiple threads.
/// </para>
/// </remarks>
public sealed class ResiliencyPipelineFactory : IResiliencyPipelineFactory
{
    private readonly ILogger<ResiliencyPipelineFactory> _logger;
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelineCache;
    private readonly ConcurrentDictionary<string, object> _genericPipelineCache;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencyPipelineFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for this factory.</param>
    public ResiliencyPipelineFactory(ILogger<ResiliencyPipelineFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pipelineCache = new ConcurrentDictionary<string, ResiliencePipeline>(StringComparer.Ordinal);
        _genericPipelineCache = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);

        ResiliencyLog.FactoryInitialized(_logger, ResiliencyPolicies.All().Count);
    }

    /// <inheritdoc />
    public IGenericResult<ResiliencePipeline> GetOrCreate(
        IResiliencyPolicy policy,
        string? operationName = null)
    {
        if (policy == null)
        {
            return GenericResult<ResiliencePipeline>.Failure(
                ResiliencyLog.InvalidPolicyConfiguration(_logger, "null", "Policy cannot be null"));
        }

        var cacheKey = policy.Name;
        operationName ??= policy.Name;

        if (_pipelineCache.TryGetValue(cacheKey, out var cachedPipeline))
        {
            ResiliencyLog.PipelineRetrievedFromCache(_logger, cacheKey);
            return GenericResult<ResiliencePipeline>.Success(cachedPipeline);
        }

        try
        {
            ResiliencyLog.CreatingPipeline(_logger, policy.Name);

            var pipeline = Create(policy, operationName);

            if (_pipelineCache.TryAdd(cacheKey, pipeline))
            {
                ResiliencyLog.PipelineCreatedAndCached(_logger, cacheKey);
            }

            ResiliencyLog.PolicyRegistered(_logger, policy.Name, policy.MaxRetries);
            return GenericResult<ResiliencePipeline>.Success(pipeline);
        }
        catch (Exception ex)
        {
            return GenericResult<ResiliencePipeline>.Failure(
                ResiliencyLog.PipelineExecutionException(_logger, ex, operationName));
        }
    }

    /// <inheritdoc />
    public IGenericResult<ResiliencePipeline> GetOrCreate(
        string policyName,
        string? operationName = null)
    {
        if (string.IsNullOrEmpty(policyName))
        {
            return GenericResult<ResiliencePipeline>.Failure(
                ResiliencyLog.InvalidPolicyConfiguration(_logger, "empty", "Policy name cannot be null or empty"));
        }

        var policy = ResiliencyPolicies.ByName(policyName);
        if (policy == ResiliencyPolicies.NotFound)
        {
            return GenericResult<ResiliencePipeline>.Failure(
                ResiliencyLog.PolicyNotFound(_logger, policyName));
        }

        return GetOrCreate(policy, operationName ?? policyName);
    }

    /// <inheritdoc />
    public IGenericResult<ResiliencePipeline<TResult>> GetOrCreatePipeline<TResult>(
        IResiliencyPolicy policy,
        string? operationName = null)
    {
        if (policy == null)
        {
            return GenericResult<ResiliencePipeline<TResult>>.Failure(
                ResiliencyLog.InvalidPolicyConfiguration(_logger, "null", "Policy cannot be null"));
        }

        var cacheKey = $"{policy.Name}_{typeof(TResult).FullName}";
        operationName ??= policy.Name;

        if (_genericPipelineCache.TryGetValue(cacheKey, out var cached) && cached is ResiliencePipeline<TResult> cachedPipeline)
        {
            ResiliencyLog.PipelineRetrievedFromCache(_logger, cacheKey);
            return GenericResult<ResiliencePipeline<TResult>>.Success(cachedPipeline);
        }

        try
        {
            ResiliencyLog.CreatingPipeline(_logger, policy.Name);

            var pipeline = CreateGenericPipeline<TResult>(policy, operationName);

            if (_genericPipelineCache.TryAdd(cacheKey, pipeline))
            {
                ResiliencyLog.PipelineCreatedAndCached(_logger, cacheKey);
            }

            ResiliencyLog.PolicyRegistered(_logger, policy.Name, policy.MaxRetries);
            return GenericResult<ResiliencePipeline<TResult>>.Success(pipeline);
        }
        catch (Exception ex)
        {
            return GenericResult<ResiliencePipeline<TResult>>.Failure(
                ResiliencyLog.PipelineExecutionException(_logger, ex, operationName));
        }
    }

    /// <inheritdoc />
    public IGenericResult<ResiliencePipeline<TResult>> GetOrCreatePipeline<TResult>(
        string policyName,
        string? operationName = null)
    {
        if (string.IsNullOrEmpty(policyName))
        {
            return GenericResult<ResiliencePipeline<TResult>>.Failure(
                ResiliencyLog.InvalidPolicyConfiguration(_logger, "empty", "Policy name cannot be null or empty"));
        }

        var policy = ResiliencyPolicies.ByName(policyName);
        if (policy == ResiliencyPolicies.NotFound)
        {
            return GenericResult<ResiliencePipeline<TResult>>.Failure(
                ResiliencyLog.PolicyNotFound(_logger, policyName));
        }

        return GetOrCreatePipeline<TResult>(policy, operationName ?? policyName);
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _pipelineCache.Clear();
        _genericPipelineCache.Clear();
    }

    private ResiliencePipeline Create(IResiliencyPolicy policy, string operationName)
    {
        ResiliencyLog.PolicyConfiguration(
            _logger,
            policy.Name,
            policy.MaxRetries,
            policy.InitialDelay.TotalMilliseconds,
            policy.MaxDelay.TotalMilliseconds,
            policy.CircuitBreakerThreshold);

        return new ResiliencePipelineBuilder()
            .AddRetry(CreateRetryOptions(policy, operationName))
            .AddCircuitBreaker(CreateCircuitBreakerOptions(policy, operationName))
            .AddTimeout(CreateTimeoutOptions(policy, operationName))
            .Build();
    }

    private ResiliencePipeline<TResult> CreateGenericPipeline<TResult>(IResiliencyPolicy policy, string operationName)
    {
        ResiliencyLog.PolicyConfiguration(
            _logger,
            policy.Name,
            policy.MaxRetries,
            policy.InitialDelay.TotalMilliseconds,
            policy.MaxDelay.TotalMilliseconds,
            policy.CircuitBreakerThreshold);

        return new ResiliencePipelineBuilder<TResult>()
            .AddRetry(CreateGenericRetryOptions<TResult>(policy, operationName))
            .AddCircuitBreaker(CreateGenericCircuitBreakerOptions<TResult>(policy, operationName))
            .AddTimeout(CreateTimeoutOptions(policy, operationName))
            .Build();
    }

    private RetryStrategyOptions CreateRetryOptions(IResiliencyPolicy policy, string operationName)
    {
        return new RetryStrategyOptions
        {
            MaxRetryAttempts = policy.MaxRetries,
            Delay = policy.InitialDelay,
            MaxDelay = policy.MaxDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = OnRetryCallback(operationName, policy.MaxRetries)
        };
    }

    private RetryStrategyOptions<TResult> CreateGenericRetryOptions<TResult>(IResiliencyPolicy policy, string operationName)
    {
        return new RetryStrategyOptions<TResult>
        {
            MaxRetryAttempts = policy.MaxRetries,
            Delay = policy.InitialDelay,
            MaxDelay = policy.MaxDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = OnGenericRetryCallback<TResult>(operationName, policy.MaxRetries)
        };
    }

    private CircuitBreakerStrategyOptions CreateCircuitBreakerOptions(IResiliencyPolicy policy, string operationName)
    {
        return new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = policy.CircuitBreakerThreshold,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = policy.CircuitBreakerDuration,
            OnOpened = OnCircuitOpenedCallback(operationName, policy.CircuitBreakerDuration),
            OnClosed = OnCircuitClosedCallback(operationName),
            OnHalfOpened = OnCircuitHalfOpenedCallback(operationName)
        };
    }

    private CircuitBreakerStrategyOptions<TResult> CreateGenericCircuitBreakerOptions<TResult>(IResiliencyPolicy policy, string operationName)
    {
        return new CircuitBreakerStrategyOptions<TResult>
        {
            FailureRatio = 0.5,
            MinimumThroughput = policy.CircuitBreakerThreshold,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = policy.CircuitBreakerDuration,
            OnOpened = OnGenericCircuitOpenedCallback<TResult>(operationName, policy.CircuitBreakerDuration),
            OnClosed = OnGenericCircuitClosedCallback<TResult>(operationName),
            OnHalfOpened = OnGenericCircuitHalfOpenedCallback<TResult>(operationName)
        };
    }

    private TimeoutStrategyOptions CreateTimeoutOptions(IResiliencyPolicy policy, string operationName)
    {
        // Use a reasonable default timeout based on the policy's max delay and retries
        var timeout = TimeSpan.FromSeconds(Math.Max(30, policy.MaxDelay.TotalSeconds * (policy.MaxRetries + 1)));

        ResiliencyLog.TimeoutConfigured(_logger, (int)timeout.TotalSeconds, operationName);

        return new TimeoutStrategyOptions
        {
            Timeout = timeout,
            OnTimeout = OnTimeoutCallback(operationName)
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Callback Handlers
    // ═══════════════════════════════════════════════════════════════════════════

    [ExcludeFromCodeCoverage(Justification = "Retry callback - tested via integration tests")]
    private Func<OnRetryArguments<object>, ValueTask> OnRetryCallback(string operationName, int maxRetries)
    {
        return args =>
        {
            ResiliencyLog.RetryAttempt(_logger, args.AttemptNumber, maxRetries, operationName);
            ResiliencyLog.RetryDelayApplied(_logger, args.RetryDelay.TotalMilliseconds, operationName);
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Retry callback - tested via integration tests")]
    private Func<OnRetryArguments<TResult>, ValueTask> OnGenericRetryCallback<TResult>(string operationName, int maxRetries)
    {
        return args =>
        {
            ResiliencyLog.RetryAttempt(_logger, args.AttemptNumber, maxRetries, operationName);
            ResiliencyLog.RetryDelayApplied(_logger, args.RetryDelay.TotalMilliseconds, operationName);
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private Func<OnCircuitOpenedArguments<object>, ValueTask> OnCircuitOpenedCallback(string operationName, TimeSpan duration)
    {
        return _ =>
        {
            ResiliencyLog.CircuitBreakerOpened(_logger, operationName, (int)duration.TotalSeconds);
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private Func<OnCircuitOpenedArguments<TResult>, ValueTask> OnGenericCircuitOpenedCallback<TResult>(string operationName, TimeSpan duration)
    {
        return _ =>
        {
            ResiliencyLog.CircuitBreakerOpened(_logger, operationName, (int)duration.TotalSeconds);
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private Func<OnCircuitClosedArguments<object>, ValueTask> OnCircuitClosedCallback(string operationName)
    {
        return _ =>
        {
            ResiliencyLog.CircuitBreakerClosed(_logger, operationName);
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private Func<OnCircuitClosedArguments<TResult>, ValueTask> OnGenericCircuitClosedCallback<TResult>(string operationName)
    {
        return _ =>
        {
            ResiliencyLog.CircuitBreakerClosed(_logger, operationName);
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private Func<OnCircuitHalfOpenedArguments, ValueTask> OnCircuitHalfOpenedCallback(string operationName)
    {
        return _ =>
        {
            ResiliencyLog.CircuitBreakerHalfOpen(_logger, operationName);
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Circuit breaker callback - tested via integration tests")]
    private Func<OnCircuitHalfOpenedArguments, ValueTask> OnGenericCircuitHalfOpenedCallback<TResult>(string operationName)
    {
        return _ =>
        {
            ResiliencyLog.CircuitBreakerHalfOpen(_logger, operationName);
            return ValueTask.CompletedTask;
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Timeout callback - tested via integration tests")]
    private Func<OnTimeoutArguments, ValueTask> OnTimeoutCallback(string operationName)
    {
        return args =>
        {
            ResiliencyLog.OperationTimedOut(_logger, operationName, (int)args.Timeout.TotalSeconds);
            return ValueTask.CompletedTask;
        };
    }
}
