using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Orchestration.Abstractions;
using Fdw.Orchestration.Abstractions.Caching;
using Fdw.Orchestration.Abstractions.Logging;
using Fdw.Orchestration.Abstractions.Resilience;
using Fdw.Orchestration.Abstractions.Results;
using Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;
using Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;
using Fdw.Orchestration.Abstractions.TypeCollections.ExecutionStatusOptions;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Fdw.Orchestration.Abstractions.TypeCollections;

namespace Fdw.Orchestration.Execution;

/// <summary>
/// Executes orchestrations with step sequencing, error handling, retry logic, and resilience.
/// </summary>
/// <remarks>
/// The executor handles:
/// - Step dependency resolution using topological sort
/// - Error handling mode dispatch (StopOnError, SkipAndContinue, RetryWithBackoff, RedirectToDeadLetter)
/// - Resilience pipeline integration via <see cref="IResiliencePipelineFactory"/>
/// - State tracking for pause/resume/cancel operations
/// - Metrics aggregation across all steps
/// </remarks>
public class OrchestrationExecutor : IOrchestrationExecutor
{
    private readonly IResiliencePipelineFactory _resilienceFactory;
    private readonly IOrchestrationCache? _cache;
    private readonly ILogger<OrchestrationExecutor> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Track active executions for pause/resume/cancel
    private readonly ConcurrentDictionary<string, ExecutionState> _activeExecutions = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationExecutor"/> class.
    /// </summary>
    /// <param name="resilienceFactory">The resilience pipeline factory.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="cache">Optional orchestration cache.</param>
    /// <param name="logger">Optional logger.</param>
    public OrchestrationExecutor(
        IResiliencePipelineFactory resilienceFactory,
        IServiceProvider serviceProvider,
        IOrchestrationCache? cache = null,
        ILogger<OrchestrationExecutor>? logger = null)
    {
        _resilienceFactory = resilienceFactory ?? throw new ArgumentNullException(nameof(resilienceFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _cache = cache;
        _logger = logger ?? NullLogger<OrchestrationExecutor>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IOrchestrationResult>> Execute(
        IOrchestration orchestration,
        IOrchestrationContext context,
        CancellationToken cancellationToken = default)
    {
        if (orchestration == null) throw new ArgumentNullException(nameof(orchestration));
        if (context == null) throw new ArgumentNullException(nameof(context));

        OrchestrationLogger.OrchestrationStarted(
            _logger,
            context.ExecutionId.ToString(),
            orchestration.OrchestrationId,
            orchestration.Name);

        var validationResult = await ValidateOrchestration(orchestration, context, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsSuccess)
            return validationResult;

        var (state, result) = InitializeExecution(context, orchestration);

        try
        {
            return await ExecuteOrchestrationCore(orchestration, context, state, result, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            // Why: cancellation is an expected outcome at this boundary; ex is named so the caught
            // exception is observed (FDW022) but no additional logging is needed — the handler
            // already records the Cancelled status.
            _ = ex;
            return HandleCancellation(result, context, orchestration);
        }
        catch (Exception ex)
        {
            return HandleExecutionException(result, context, orchestration, ex);
        }
        finally
        {
            _activeExecutions.TryRemove(context.ExecutionId.ToString(), out _);
        }
    }

    private async Task<IGenericResult<IOrchestrationResult>> ExecuteOrchestrationCore(
        IOrchestration orchestration,
        IOrchestrationContext context,
        ExecutionState state,
        OrchestrationResult result,
        CancellationToken cancellationToken)
    {
        var executionOrder = ResolveExecutionOrder(orchestration);
        var defaultErrorHandling = context.Policy.DefaultErrorHandling
            ?? ErrorHandlingModes.ByName("StopOnError");

        var stepResults = await ExecuteStepsInOrder(
            executionOrder,
            defaultErrorHandling,
            state,
            context,
            result,
            cancellationToken).ConfigureAwait(false);

        if (stepResults == null)
            return GenericResult<IOrchestrationResult>.Success(result);

        if (context is OrchestrationExecutionContext finalContext)
            finalContext.SetCurrentStep(null);

        CompleteFinalResult(result, stepResults, context);

        return GenericResult<IOrchestrationResult>.Success(result);
    }

    private IGenericResult<IOrchestrationResult> HandleCancellation(
        OrchestrationResult result,
        IOrchestrationContext context,
        IOrchestration orchestration)
    {
        OrchestrationLogger.OrchestrationCancelled(_logger, context.ExecutionId.ToString(), orchestration.OrchestrationId);
        result.Complete(
            ExecutionStatuses.ByName("Cancelled"),
            "Execution was cancelled");

        return GenericResult<IOrchestrationResult>.Success(result);
    }

    private IGenericResult<IOrchestrationResult> HandleExecutionException(
        OrchestrationResult result,
        IOrchestrationContext context,
        IOrchestration orchestration,
        Exception ex)
    {
        OrchestrationLogger.OrchestrationFailed(
            _logger,
            ex,
            context.ExecutionId.ToString(),
            orchestration.OrchestrationId,
            null,
            ex.Message);

        result.Complete(
            ExecutionStatuses.ByName("Failed"),
            ex.Message,
            ex);

        return GenericResult<IOrchestrationResult>.Failure(
            OrchestrationResultCodes.ByName("ExecutionFailed"),
            ResultDetails.Create().With("ErrorMessage", ex.Message));
    }

    private async Task<IGenericResult<IOrchestrationResult>> ValidateOrchestration(
        IOrchestration orchestration,
        IOrchestrationContext context,
        CancellationToken cancellationToken)
    {
        var validationResult = await orchestration.Validate(cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsSuccess)
        {
            OrchestrationLogger.OrchestrationFailed(
                _logger,
                null,
                context.ExecutionId.ToString(),
                orchestration.OrchestrationId,
                null,
                $"Validation failed: {validationResult.CurrentMessage}");
            return GenericResult<IOrchestrationResult>.Failure(
                OrchestrationResultCodes.ByName("ValidationFailed"),
                ResultDetails.Create().With("ValidationMessage", validationResult.CurrentMessage));
        }

        return GenericResult<IOrchestrationResult>.Success(null!);
    }

    private (ExecutionState State, OrchestrationResult Result) InitializeExecution(
        IOrchestrationContext context,
        IOrchestration orchestration)
    {
        var executionIdString = context.ExecutionId.ToString();
        var state = new ExecutionState(executionIdString, orchestration.OrchestrationId);
        _activeExecutions[executionIdString] = state;

        var result = new OrchestrationResult(
            executionIdString,
            orchestration.OrchestrationId,
            ExecutionStatuses.ByName("Running"),
            context.StartTime);

        return (state, result);
    }

    private async Task<List<IOrchestrationStepResult>?> ExecuteStepsInOrder(
        List<IOrchestrationStep> executionOrder,
        IErrorHandlingMode defaultErrorHandling,
        ExecutionState state,
        IOrchestrationContext context,
        OrchestrationResult result,
        CancellationToken cancellationToken)
    {
        var stepResults = new List<IOrchestrationStepResult>();

        foreach (var step in executionOrder)
        {
            if (ShouldCancelExecution(state, context, result, cancellationToken))
                return null;

            if (await WaitWhilePaused(state, result, cancellationToken).ConfigureAwait(false))
                return null;

            if (TrySkipStep(step, stepResults, result, context))
                continue;

            UpdateExecutionState(state, context, step, executionOrder.Count, stepResults.Count);

            var errorHandling = step.ErrorHandling ?? defaultErrorHandling;
            var stepResult = await ExecuteStepWithErrorHandling(
                step, context, errorHandling, cancellationToken).ConfigureAwait(false);

            stepResults.Add(stepResult);
            result.AddStepResult(stepResult);

            if (context is OrchestrationExecutionContext ctx)
                ctx.RecordStepResult(step.StepId, stepResult);

            if (HandleStepFailure(stepResult, errorHandling, context, result, step))
                return null;
        }

        return stepResults;
    }

    private bool ShouldCancelExecution(
        ExecutionState state,
        IOrchestrationContext context,
        OrchestrationResult result,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested && !state.IsCancellationRequested)
            return false;

        OrchestrationLogger.OrchestrationCancelled(_logger, context.ExecutionId.ToString(), result.OrchestrationId);
        result.Complete(ExecutionStatuses.ByName("Cancelled"), "Execution was cancelled");
        return true;
    }

    private static async Task<bool> WaitWhilePaused(
        ExecutionState state,
        OrchestrationResult result,
        CancellationToken cancellationToken)
    {
        while (state.IsPaused)
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested && !state.IsCancellationRequested)
                continue;

            result.Complete(
                ExecutionStatuses.ByName("Cancelled"),
                "Execution was cancelled while paused");
            return true;
        }

        return false;
    }

    private bool TrySkipStep(
        IOrchestrationStep step,
        List<IOrchestrationStepResult> stepResults,
        OrchestrationResult result,
        IOrchestrationContext context)
    {
        if (!step.IsEnabled)
        {
            AddSkippedResult(step, stepResults, result, context, "Step is disabled");
            return true;
        }

        if (!AreDependenciesSatisfied(step, stepResults))
        {
            AddSkippedResult(step, stepResults, result, context, "One or more dependencies failed");
            return true;
        }

        return false;
    }

    private void AddSkippedResult(
        IOrchestrationStep step,
        List<IOrchestrationStepResult> stepResults,
        OrchestrationResult result,
        IOrchestrationContext context,
        string reason)
    {
        var skippedResult = OrchestrationStepResult.Skipped(step.StepId, step.Name, reason);
        stepResults.Add(skippedResult);
        result.AddStepResult(skippedResult);
        OrchestrationLogger.StepSkipped(_logger, context.ExecutionId.ToString(), step.StepId, reason);
    }

    private static void UpdateExecutionState(
        ExecutionState state,
        IOrchestrationContext context,
        IOrchestrationStep step,
        int totalSteps,
        int completedSteps)
    {
        if (context is OrchestrationExecutionContext execContext)
            execContext.SetCurrentStep(step);

        state.CurrentStepId = step.StepId;
        state.TotalSteps = totalSteps;
        state.CompletedSteps = completedSteps;
    }

    private bool HandleStepFailure(
        IOrchestrationStepResult stepResult,
        IErrorHandlingMode errorHandling,
        IOrchestrationContext context,
        OrchestrationResult result,
        IOrchestrationStep step)
    {
        if (!stepResult.Status.IsFailure || errorHandling.ContinuesExecution)
            return false;

        OrchestrationLogger.OrchestrationFailed(
            _logger,
            stepResult.Exception,
            context.ExecutionId.ToString(),
            result.OrchestrationId,
            step.StepId,
            stepResult.ErrorMessage ?? "Step failed");

        result.Complete(
            ExecutionStatuses.ByName("Failed"),
            stepResult.ErrorMessage,
            stepResult.Exception);

        return true;
    }

    private void CompleteFinalResult(
        OrchestrationResult result,
        List<IOrchestrationStepResult> stepResults,
        IOrchestrationContext context)
    {
        var finalStatus = stepResults.Any(r => r.Status.HasWarnings || r.Status.IsFailure)
            ? ExecutionStatuses.ByName("SucceededWithWarnings")
            : ExecutionStatuses.ByName("Succeeded");

        var lastOutput = stepResults
            .LastOrDefault(r => r.Status.IsSuccess)?.Output;

        result.Output = lastOutput;
        result.Complete(finalStatus);

        OrchestrationLogger.OrchestrationCompleted(
            _logger,
            context.ExecutionId.ToString(),
            finalStatus.Name,
            result.Duration,
            stepResults.Count);
    }

    /// <summary>
    /// Resolves the execution order of steps using topological sort.
    /// </summary>
    // MA0051: Method length acceptable - topological sort algorithm (Kahn's algorithm for dependency resolution)
#pragma warning disable MA0051 // Method is too long
    private static List<IOrchestrationStep> ResolveExecutionOrder(IOrchestration orchestration)
#pragma warning restore MA0051
    {
        var steps = orchestration.Phases.ToDictionary(s => s.StepId, StringComparer.Ordinal);
        var inDegree = new Dictionary<string, int>(StringComparer.Ordinal);
        var adjacency = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        // Initialize
        foreach (var step in orchestration.Phases)
        {
            inDegree[step.StepId] = 0;
            adjacency[step.StepId] = [];
        }

        // Build graph
        foreach (var step in orchestration.Phases)
        {
            foreach (var dependency in step.DependsOn)
            {
                if (adjacency.TryGetValue(dependency, out var dependents))
                {
                    dependents.Add(step.StepId);
                    inDegree[step.StepId]++;
                }
            }
        }

        // Kahn's algorithm
        var queue = new Queue<string>();
        foreach (var kvp in inDegree.Where(x => x.Value == 0))
        {
            queue.Enqueue(kvp.Key);
        }

        var result = new List<IOrchestrationStep>();

        while (queue.Count > 0)
        {
            var stepId = queue.Dequeue();
            result.Add(steps[stepId]);

            foreach (var dependent in adjacency[stepId])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        // Check for cycles
        if (result.Count != orchestration.Phases.Count)
        {
            throw new InvalidOperationException(
                "Circular dependency detected in orchestration steps");
        }

        // Secondary sort by sequence number for steps at the same level
        return result
            .OrderBy(s => result.IndexOf(s))
            .ThenBy(s => s.SequenceNumber)
            .ToList();
    }

    /// <summary>
    /// Checks if all dependencies of a step have completed successfully.
    /// </summary>
    private static bool AreDependenciesSatisfied(
        IOrchestrationStep step,
        IReadOnlyCollection<IOrchestrationStepResult> completedResults)
    {
        if (step.DependsOn.Count == 0)
        {
            return true;
        }

        var completedSteps = completedResults.ToDictionary(r => r.StepId, StringComparer.Ordinal);

        return step.DependsOn.All(depId =>
            completedSteps.TryGetValue(depId, out var depResult) && depResult.Status.IsSuccess);
    }

    /// <summary>
    /// Executes a step with error handling and resilience.
    /// </summary>
    private async Task<IOrchestrationStepResult> ExecuteStepWithErrorHandling(
        IOrchestrationStep step,
        IOrchestrationContext context,
        IErrorHandlingMode errorHandling,
        CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;

        OrchestrationLogger.StepStarted(_logger, context.ExecutionId.ToString(), step.StepId, step.Name, 1);

        try
        {
            return await DispatchStepExecution(step, context, errorHandling, startTime, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            return OrchestrationStepResult.Failure(
                step.StepId, step.Name, startTime, "Step was cancelled", ex, retryAttempts: 0);
        }
        catch (TimeoutException ex)
        {
            // Why: ex is named so the caught exception is observed (FDW022); TimedOut carries the
            // structured status — the exception itself is recorded on the failure result for context.
            return OrchestrationStepResult.Failure(
                step.StepId, step.Name, startTime,
                $"Step timed out after {(step.Timeout ?? TimeSpan.FromMinutes(5)).TotalSeconds:F1} seconds",
                ex, retryAttempts: 0);
        }
        catch (Exception ex)
        {
            return HandleStepException(step, context, startTime, ex);
        }
    }

    private async Task<IOrchestrationStepResult> DispatchStepExecution(
        IOrchestrationStep step,
        IOrchestrationContext context,
        IErrorHandlingMode errorHandling,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        var stepContext = new OrchestrationStepExecutionContext(
            $"{context.ExecutionId:N}-{step.StepId}",
            step,
            context,
            1);

        var cachedResult = await TryGetCachedStepResult(step, context, stepContext, cancellationToken)
            .ConfigureAwait(false);
        if (cachedResult != null)
            return cachedResult;

        var resilienceOptions = BuildResilienceOptions(step, errorHandling);

        if (resilienceOptions.MaxRetryAttempts > 0)
        {
            return await ExecuteWithResilience(
                step, context, stepContext, errorHandling, resilienceOptions,
                startTime, cancellationToken).ConfigureAwait(false);
        }

        return await ExecuteWithoutResilience(
            step, context, stepContext, errorHandling,
            startTime, cancellationToken).ConfigureAwait(false);
    }

    private OrchestrationStepResult HandleStepException(
        IOrchestrationStep step,
        IOrchestrationContext context,
        DateTimeOffset startTime,
        Exception ex)
    {
        OrchestrationLogger.StepFailed(
            _logger, ex, context.ExecutionId.ToString(), step.StepId, 1, ex.Message);

        return OrchestrationStepResult.Failure(
            step.StepId, step.Name, startTime, ex.Message, ex, 0);
    }

    private async Task<IOrchestrationStepResult?> TryGetCachedStepResult(
        IOrchestrationStep step,
        IOrchestrationContext context,
        OrchestrationStepExecutionContext stepContext,
        CancellationToken cancellationToken)
    {
        if (!step.IsCacheable || _cache == null)
            return null;

        var cacheKey = BuildStepCacheKey(context.ExecutionId.ToString(), step.StepId, stepContext.InputData);
        var cachedResult = await _cache.Get<IOrchestrationStepResult>(cacheKey, cancellationToken)
            .ConfigureAwait(false);

        if (cachedResult != null)
        {
            OrchestrationLogger.CacheHit(_logger, cacheKey, "StepResult");
            return new OrchestrationStepResult(
                step.StepId,
                step.Name,
                cachedResult.Status,
                cachedResult.StartTime,
                cachedResult.EndTime)
            {
                Output = cachedResult.Output,
                RecordsProcessed = cachedResult.RecordsProcessed,
                WasCached = true
            };
        }

        return null;
    }

    private async Task<IOrchestrationStepResult> ExecuteWithResilience(
        IOrchestrationStep step,
        IOrchestrationContext context,
        OrchestrationStepExecutionContext stepContext,
        IErrorHandlingMode errorHandling,
        ResilienceOptions resilienceOptions,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        var pipeline = _resilienceFactory.Create<object?>(resilienceOptions);
        var retryAttempts = 0;

        try
        {
            var output = await pipeline.ExecuteAsync(async ct =>
            {
                retryAttempts = stepContext.AttemptNumber - 1;
                var stepOutput = await ExecuteStepCore(step, stepContext, ct).ConfigureAwait(false);
                stepContext.IncrementAttempt();
                return stepOutput;
            }, cancellationToken).ConfigureAwait(false);

            var result = OrchestrationStepResult.Success(
                step.StepId, step.Name, startTime, output, retryAttempts: retryAttempts);

            await CacheStepResultIfApplicable(step, context, stepContext, result, cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            retryAttempts = stepContext.AttemptNumber - 1;
            return await HandleErrorHandlingDispatch(
                step, context, stepContext, errorHandling, startTime, ex, retryAttempts, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<IOrchestrationStepResult> ExecuteWithoutResilience(
        IOrchestrationStep step,
        IOrchestrationContext context,
        OrchestrationStepExecutionContext stepContext,
        IErrorHandlingMode errorHandling,
        DateTimeOffset startTime,
        CancellationToken cancellationToken)
    {
        try
        {
            var output = await ExecuteStepCore(step, stepContext, cancellationToken).ConfigureAwait(false);

            var result = OrchestrationStepResult.Success(
                step.StepId, step.Name, startTime, output);

            await CacheStepResultIfApplicable(step, context, stepContext, result, cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            return await HandleErrorHandlingDispatch(
                step, context, stepContext, errorHandling, startTime, ex, 0, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<IOrchestrationStepResult> HandleErrorHandlingDispatch(
        IOrchestrationStep step,
        IOrchestrationContext context,
        OrchestrationStepExecutionContext stepContext,
        IErrorHandlingMode errorHandling,
        DateTimeOffset startTime,
        Exception ex,
        int retryAttempts,
        CancellationToken cancellationToken)
    {
        OrchestrationLogger.StepFailed(
            _logger, ex, context.ExecutionId.ToString(), step.StepId, retryAttempts + 1, ex.Message);

        var errorResult = await errorHandling.HandleError(ex, stepContext, cancellationToken)
            .ConfigureAwait(false);

        if (errorHandling.ContinuesExecution)
        {
            return OrchestrationStepResult.SuccessWithWarnings(
                step.StepId, step.Name, startTime,
                $"Step failed but execution continues: {ex.Message}",
                retryAttempts: retryAttempts);
        }

        return OrchestrationStepResult.Failure(
            step.StepId, step.Name, startTime,
            errorResult.CurrentMessage ?? ex.Message, ex, retryAttempts);
    }

    /// <summary>
    /// Core step execution - to be overridden by derived classes or step executors.
    /// </summary>
    protected virtual Task<object?> ExecuteStepCore(
        IOrchestrationStep step,
        OrchestrationStepExecutionContext context,
        CancellationToken cancellationToken)
    {
        // Default implementation - derived classes or step executors should provide actual logic
        // For now, we'll look for a registered step executor
        var executorType = typeof(IOrchestrationStepExecutor);
        var executor = context.OrchestrationContext.Services.GetService(executorType) as IOrchestrationStepExecutor;

        if (executor != null)
        {
            return ExecuteWithStepExecutor(executor, step, context, cancellationToken);
        }

        // No executor found - return null output (step did nothing)
        OrchestrationLogger.NoStepExecutorFound(
            _logger,
            context.OrchestrationContext.ExecutionId.ToString(),
            step.StepId);

        return Task.FromResult<object?>(null);
    }

    private static async Task<object?> ExecuteWithStepExecutor(
        IOrchestrationStepExecutor executor,
        IOrchestrationStep step,
        OrchestrationStepExecutionContext context,
        CancellationToken cancellationToken)
    {
        var input = context.InputData.TryGetValue("previousOutput", out var prevOutput) ? prevOutput : null;

        var result = await executor.Execute(step, context.OrchestrationContext, input, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.CurrentMessage ?? "Step executor returned failure");
        }

        return result.Value?.Output;
    }

    /// <summary>
    /// Builds resilience options from step configuration.
    /// </summary>
    private static ResilienceOptions BuildResilienceOptions(
        IOrchestrationStep step,
        IErrorHandlingMode errorHandling)
    {
        var options = new ResilienceOptions
        {
            Timeout = step.Timeout,
            ErrorHandlingMode = errorHandling
        };

        if (errorHandling.SupportsRetry)
        {
            options.MaxRetryAttempts = (step.Configuration as OrchestrationStepConfiguration)?.MaxRetries ?? 3;

            // Use exponential backoff by default for retry modes
            options.BackoffStrategy = BackoffStrategies.ByName("Exponential");
        }
        else
        {
            options.MaxRetryAttempts = 0;
        }

        return options;
    }

    /// <summary>
    /// Caches the step result if the step is cacheable.
    /// </summary>
    private async Task CacheStepResultIfApplicable(
        IOrchestrationStep step,
        IOrchestrationContext context,
        OrchestrationStepExecutionContext stepContext,
        OrchestrationStepResult result,
        CancellationToken cancellationToken)
    {
        if (!step.IsCacheable || _cache == null || !result.Status.IsSuccess)
        {
            return;
        }

        var cacheKey = BuildStepCacheKey(context.ExecutionId.ToString(), step.StepId, stepContext.InputData);
        var cacheDuration = context.Policy.ResultCacheDuration ?? TimeSpan.FromHours(1);

        await _cache.Set(
            cacheKey,
            result,
            CacheEntryOptions.AbsoluteExpiring(cacheDuration),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a cache key for a step result.
    /// </summary>
    private static string BuildStepCacheKey(
        string executionId,
        string stepId,
        IReadOnlyDictionary<string, object?> inputData)
    {
        // Create a simple hash of input data for cache key
        var inputHash = inputData.GetHashCode();
        return $"orchestration:step:{stepId}:input:{inputHash}";
    }

    /// <summary>
    /// Pauses execution of an orchestration.
    /// </summary>
    /// <param name="executionId">The execution ID to pause.</param>
    /// <returns>Result indicating success or failure.</returns>
    public IGenericResult Pause(string executionId)
    {
        if (_activeExecutions.TryGetValue(executionId, out var state))
        {
            state.IsPaused = true;
            return GenericResult.Success(OrchestrationLogger.OrchestrationPaused(
                _logger,
                executionId,
                state.OrchestrationId,
                state.CurrentStepId));
        }

        return GenericResult.Failure(OrchestrationLogger.ExecutionNotFound(_logger, executionId));
    }

    /// <summary>
    /// Resumes a paused execution.
    /// </summary>
    /// <param name="executionId">The execution ID to resume.</param>
    /// <returns>Result indicating success or failure.</returns>
    public IGenericResult Resume(string executionId)
    {
        if (_activeExecutions.TryGetValue(executionId, out var state))
        {
            state.IsPaused = false;
            return GenericResult.Success(OrchestrationLogger.OrchestrationResumed(
                _logger,
                executionId,
                state.OrchestrationId,
                state.CurrentStepId));
        }

        return GenericResult.Failure(OrchestrationLogger.ExecutionNotFound(_logger, executionId));
    }

    /// <summary>
    /// Cancels execution of an orchestration.
    /// </summary>
    /// <param name="executionId">The execution ID to cancel.</param>
    /// <returns>Result indicating success or failure.</returns>
    public IGenericResult Cancel(string executionId)
    {
        if (_activeExecutions.TryGetValue(executionId, out var state))
        {
            state.IsCancellationRequested = true;
            return GenericResult.Success(OrchestrationLogger.CancellationRequested(
                _logger,
                executionId,
                state.OrchestrationId));
        }

        return GenericResult.Failure(OrchestrationLogger.ExecutionNotFound(_logger, executionId));
    }

    /// <summary>
    /// Gets the status of an execution.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <returns>The execution status, or null if not found.</returns>
    public ExecutionState? GetStatus(string executionId)
    {
        return _activeExecutions.TryGetValue(executionId, out var state) ? state : null;
    }

    /// <summary>
    /// Tracks the state of an active execution.
    /// </summary>
    public sealed class ExecutionState
    {
        /// <summary>
        /// Gets the execution ID.
        /// </summary>
        public string ExecutionId { get; }

        /// <summary>
        /// Gets the orchestration ID.
        /// </summary>
        public string OrchestrationId { get; }

        /// <summary>
        /// Gets or sets whether execution is paused.
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Gets or sets whether cancellation has been requested.
        /// </summary>
        public bool IsCancellationRequested { get; set; }

        /// <summary>
        /// Gets or sets the current step being executed.
        /// </summary>
        public string? CurrentStepId { get; set; }

        /// <summary>
        /// Gets or sets the total number of steps.
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// Gets or sets the number of completed steps.
        /// </summary>
        public int CompletedSteps { get; set; }

        /// <summary>
        /// Gets the percent complete.
        /// </summary>
        public double PercentComplete => TotalSteps > 0 ? (double)CompletedSteps / TotalSteps * 100 : 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionState"/> class.
        /// </summary>
        public ExecutionState(string executionId, string orchestrationId)
        {
            ExecutionId = executionId;
            OrchestrationId = orchestrationId;
        }
    }
}
