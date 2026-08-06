using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Fdw.Orchestration.Abstractions;

namespace Fdw.Orchestration.Execution;

/// <summary>
/// Default implementation of <see cref="IOrchestrationStepExecutionContext"/>.
/// </summary>
/// <remarks>
/// Provides step-specific execution context, including input/output data management,
/// attempt tracking for retries, and access to the parent orchestration context.
/// </remarks>
public sealed class OrchestrationStepExecutionContext : IOrchestrationStepExecutionContext
{
    private readonly ConcurrentDictionary<string, object?> _stepState;
    private readonly ConcurrentDictionary<string, object?> _outputData;
    private readonly Dictionary<string, object?> _inputData;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationStepExecutionContext"/> class.
    /// </summary>
    /// <param name="stepExecutionId">The unique step execution identifier.</param>
    /// <param name="step">The step being executed.</param>
    /// <param name="orchestrationContext">The parent orchestration context.</param>
    /// <param name="attemptNumber">The current attempt number (1-based).</param>
    public OrchestrationStepExecutionContext(
        string stepExecutionId,
        IOrchestrationStep step,
        IOrchestrationContext orchestrationContext,
        int attemptNumber = 1)
    {
        StepExecutionId = stepExecutionId ?? throw new ArgumentNullException(nameof(stepExecutionId));
        Step = step ?? throw new ArgumentNullException(nameof(step));
        OrchestrationContext = orchestrationContext ?? throw new ArgumentNullException(nameof(orchestrationContext));
        AttemptNumber = attemptNumber;
        StartTime = DateTimeOffset.UtcNow;
        _stepState = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);
        _outputData = new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);
        _inputData = BuildInputData(step, orchestrationContext);
    }

    /// <inheritdoc/>
    public string StepExecutionId { get; }

    /// <inheritdoc/>
    public string StepId => Step.StepId;

    /// <inheritdoc/>
    public IOrchestrationStep Step { get; }

    /// <inheritdoc/>
    public IOrchestrationContext OrchestrationContext { get; }

    /// <inheritdoc/>
    public DateTimeOffset StartTime { get; }

    /// <inheritdoc/>
    public int AttemptNumber { get; private set; }

    /// <inheritdoc/>
    public IDictionary<string, object?> StepState => _stepState;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> InputData => _inputData;

    /// <inheritdoc/>
    public IDictionary<string, object?> OutputData => _outputData;

    /// <summary>
    /// Increments the attempt number for retry scenarios.
    /// </summary>
    /// <returns>The new attempt number.</returns>
    internal int IncrementAttempt()
    {
        return ++AttemptNumber;
    }

    /// <summary>
    /// Creates a new context for a retry attempt.
    /// </summary>
    /// <returns>A new context with incremented attempt number.</returns>
    internal OrchestrationStepExecutionContext CreateRetryContext()
    {
        return new OrchestrationStepExecutionContext(
            StepExecutionId,
            Step,
            OrchestrationContext,
            AttemptNumber + 1);
    }

    /// <summary>
    /// Builds input data from orchestration parameters and dependent step outputs.
    /// </summary>
    private static Dictionary<string, object?> BuildInputData(
        IOrchestrationStep step,
        IOrchestrationContext orchestrationContext)
    {
        var inputData = new Dictionary<string, object?>(StringComparer.Ordinal);

        // Include orchestration parameters
        foreach (var parameter in orchestrationContext.Parameters)
        {
            inputData[parameter.Key] = parameter.Value;
        }

        // Include outputs from dependent steps
        foreach (var dependencyId in step.DependsOn)
        {
            if (orchestrationContext.CompletedSteps.TryGetValue(dependencyId, out var dependentResult))
            {
                // Add the output with the step ID as a prefix
                inputData[$"{dependencyId}.output"] = dependentResult.Output;

                // Also add a shorthand if there's only one dependency
                if (step.DependsOn.Count == 1)
                {
                    inputData["previousOutput"] = dependentResult.Output;
                }
            }
        }

        // Include shared state
        foreach (var stateItem in orchestrationContext.SharedState)
        {
            inputData[$"state.{stateItem.Key}"] = stateItem.Value;
        }

        return inputData;
    }

    /// <summary>
    /// Gets the output from a specific dependent step.
    /// </summary>
    /// <typeparam name="T">The expected output type.</typeparam>
    /// <param name="stepId">The step ID to get output from.</param>
    /// <returns>The output value, or default if not found.</returns>
    public T? GetDependentOutput<T>(string stepId)
    {
        if (_inputData.TryGetValue($"{stepId}.output", out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    /// <summary>
    /// Gets the output from the previous step (if there's only one dependency).
    /// </summary>
    /// <typeparam name="T">The expected output type.</typeparam>
    /// <returns>The output value, or default if not found.</returns>
    public T? GetPreviousOutput<T>()
    {
        if (_inputData.TryGetValue("previousOutput", out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    /// <summary>
    /// Gets a parameter value from the orchestration context.
    /// </summary>
    /// <typeparam name="T">The expected parameter type.</typeparam>
    /// <param name="name">The parameter name.</param>
    /// <returns>The parameter value, or default if not found.</returns>
    public T? GetParameter<T>(string name)
    {
        if (_inputData.TryGetValue(name, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    /// <summary>
    /// Sets a value in the output data for subsequent steps.
    /// </summary>
    /// <param name="key">The output key.</param>
    /// <param name="value">The output value.</param>
    public void SetOutput(string key, object? value)
    {
        _outputData[key] = value;
    }

    /// <summary>
    /// Sets the primary output for this step.
    /// </summary>
    /// <param name="value">The output value.</param>
    public void SetOutput(object? value)
    {
        _outputData["result"] = value;
    }

    /// <summary>
    /// Gets the primary output from this step's output data.
    /// </summary>
    /// <typeparam name="T">The expected output type.</typeparam>
    /// <returns>The output value, or default if not found.</returns>
    public T? GetOutput<T>()
    {
        if (_outputData.TryGetValue("result", out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }
}
