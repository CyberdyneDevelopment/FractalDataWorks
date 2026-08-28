using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.Lineage;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Walks a calculation's ordered steps, binding each step's operands to its operation's declared
/// parameters and publishing the step's result under its output alias.
/// </summary>
/// <remarks>
/// This closes the gap between the configured step model and the registered calculation operations:
/// the operations already compute, the configuration already models inputs/steps/operands, and this
/// is the loop that joins them. It resolves nothing implicitly — see
/// <see cref="ICalculationStepExecutor"/> for the fail-loud contract.
/// </remarks>
public sealed class CalculationStepExecutor : ICalculationStepExecutor
{
    private const string OperandTypeInput = "Input";
    private const string OperandTypeStepReference = "StepReference";
    private const string OperandTypeLiteral = "Literal";

    private readonly ILogger<CalculationStepExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationStepExecutor"/> class.
    /// </summary>
    /// <param name="logger">The logger; a null logger is used when DI supplies none.</param>
    public CalculationStepExecutor(ILogger<CalculationStepExecutor>? logger)
    {
        _logger = logger ?? NullLogger<CalculationStepExecutor>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<object?>> Execute(
        IReadOnlyList<IGenericConfiguration> steps,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationTraceRecorder recorder,
        CancellationToken cancellationToken = default)
    {
        var orderedResult = OrderSteps(steps);
        if (orderedResult.IsFailure)
        {
            return orderedResult.ToNewResult<object?>();
        }

        var ordered = orderedResult.Value!;
        CalculationStepExecutorLog.StepExecutionStarted(_logger, ordered.Count);

        var published = new Dictionary<string, object?>(StringComparer.Ordinal);
        var lastAlias = string.Empty;

        foreach (var step in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var operandTrace = new List<CalculationOperandTrace>(step.Operands.Count);

            if (published.ContainsKey(step.OutputAlias))
            {
                var duplicate = GenericResult<object?>.Failure(
                    CalculationStepExecutorLog.DuplicateOutputAlias(_logger, step.Name, step.OutputAlias));
                RecordFailedStep(recorder, step, operandTrace, duplicate);
                return duplicate;
            }

            var stepResult = await ExecuteStep(step, inputs, published, operandTrace, cancellationToken).ConfigureAwait(false);
            if (stepResult.IsFailure)
            {
                RecordFailedStep(recorder, step, operandTrace, stepResult);
                return stepResult.ToNewResult<object?>();
            }

            published[step.OutputAlias] = stepResult.Value;
            lastAlias = step.OutputAlias;
            recorder.Record(new CalculationStepTrace
            {
                Ordinal = step.Ordinal,
                StepName = step.Name,
                OperationType = step.OperationType,
                OutputAlias = step.OutputAlias,
                Operands = operandTrace,
                OutputValue = CalculationTraceValue.From(stepResult.Value),
                Completed = true,
            });
            CalculationStepExecutorLog.StepCompleted(_logger, step.Name, step.OutputAlias);
        }

        CalculationStepExecutorLog.StepExecutionCompleted(_logger, lastAlias);
        return GenericResult<object?>.Success(published[lastAlias]);
    }

    /// <summary>
    /// Records the step an execution stopped on, with the operands it had bound before it stopped.
    /// </summary>
    private static void RecordFailedStep(
        ICalculationTraceRecorder recorder,
        CalculationStepConfiguration step,
        IReadOnlyList<CalculationOperandTrace> operandTrace,
        IGenericResult failure) =>
        recorder.Record(new CalculationStepTrace
        {
            Ordinal = step.Ordinal,
            StepName = step.Name,
            OperationType = step.OperationType,
            OutputAlias = step.OutputAlias,
            Operands = operandTrace,
            Completed = false,

            Failure = failure.Messages.Count > 0 ? failure.Messages[0] : null,
        });

    /// <summary>
    /// Narrows the weakly-typed step elements and orders them by ordinal.
    /// </summary>
    private IGenericResult<List<CalculationStepConfiguration>> OrderSteps(IReadOnlyList<IGenericConfiguration> steps)
    {
        if (steps is null || steps.Count == 0)
        {
            return GenericResult<List<CalculationStepConfiguration>>.Failure(
                CalculationStepExecutorLog.NoStepsConfigured(_logger));
        }

        var typed = new List<CalculationStepConfiguration>(steps.Count);
        foreach (var element in steps)
        {
            if (element is not CalculationStepConfiguration step)
            {
                return GenericResult<List<CalculationStepConfiguration>>.Failure(
                    CalculationStepExecutorLog.StepTypeUnexpected(
                        _logger, element?.GetType().Name ?? "null"));
            }

            typed.Add(step);
        }

        typed.Sort(static (left, right) => left.Ordinal.CompareTo(right.Ordinal));
        return GenericResult<List<CalculationStepConfiguration>>.Success(typed);
    }

    /// <summary>
    /// Binds one step's operands to its operation's parameters and invokes the operation.
    /// </summary>
    private async Task<IGenericResult<object?>> ExecuteStep(
        CalculationStepConfiguration step,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        IReadOnlyDictionary<string, object?> published,
        List<CalculationOperandTrace> operandTrace,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(step.OutputAlias))
        {
            return GenericResult<object?>.Failure(
                CalculationStepExecutorLog.OutputAliasMissing(_logger, step.Name));
        }

        var operation = CalculationOperationTypes.ByName(step.OperationType);
        if (operation == CalculationOperationTypes.NotFound)
        {
            return GenericResult<object?>.Failure(
                CalculationStepExecutorLog.UnknownOperation(_logger, step.Name, step.OperationType));
        }

        CalculationStepExecutorLog.StepStarted(_logger, step.Ordinal, step.Name, step.OperationType);

        var parametersResult = BuildParameters(step, inputs, published, operandTrace);
        if (parametersResult.IsFailure)
        {
            return parametersResult.ToNewResult<object?>();
        }

        var parameters = parametersResult.Value!;

        foreach (var definition in operation.Parameters)
        {
            if (definition.IsRequired && !parameters.ContainsKey(definition.Name))
            {
                return GenericResult<object?>.Failure(
                    CalculationStepExecutorLog.RequiredParameterMissing(
                        _logger, step.OperationType, step.Name, definition.Name));
            }
        }

        return await operation.Calculate(parameters, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves every operand of a step into the operation's name-keyed parameter dictionary.
    /// </summary>
    private IGenericResult<Dictionary<string, object?>> BuildParameters(
        CalculationStepConfiguration step,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        IReadOnlyDictionary<string, object?> published,
        List<CalculationOperandTrace> operandTrace)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var operand in step.Operands)
        {
            if (string.IsNullOrEmpty(operand.Name))
            {
                return GenericResult<Dictionary<string, object?>>.Failure(
                    CalculationStepExecutorLog.OperandNameMissing(_logger, step.Name));
            }

            var valueResult = ResolveOperand(operand, step, inputs, published);
            if (valueResult.IsFailure)
            {
                return valueResult.ToNewResult<Dictionary<string, object?>>();
            }

            parameters[operand.Name] = valueResult.Value;

            operandTrace.Add(new CalculationOperandTrace
            {
                OperandName = operand.Name,
                SourceKind = operand.OperandType,
                SourceReference = DescribeOperandSource(operand),
                FieldName = operand.FieldName,
                ResolvedValue = CalculationTraceValue.From(valueResult.Value),
            });
        }

        return GenericResult<Dictionary<string, object?>>.Success(parameters);
    }

    /// <summary>
    /// Resolves a single operand from an input alias, an earlier step's output, or a literal.
    /// </summary>
    private IGenericResult<object?> ResolveOperand(
        CalculationStepOperandConfiguration operand,
        CalculationStepConfiguration step,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        IReadOnlyDictionary<string, object?> published)
    {
        if (string.Equals(operand.OperandType, OperandTypeInput, StringComparison.Ordinal))
        {
            return ResolveInputOperand(operand, step, inputs);
        }

        if (string.Equals(operand.OperandType, OperandTypeStepReference, StringComparison.Ordinal))
        {
            return ResolveStepOperand(operand, step, published);
        }

        if (string.Equals(operand.OperandType, OperandTypeLiteral, StringComparison.Ordinal))
        {
            return string.IsNullOrEmpty(operand.LiteralValue)
                ? GenericResult<object?>.Failure(
                    CalculationStepExecutorLog.LiteralValueMissing(_logger, operand.Name, step.Name))
                : GenericResult<object?>.Success(operand.LiteralValue);
        }

        return GenericResult<object?>.Failure(
            CalculationStepExecutorLog.UnknownOperandType(
                _logger, operand.Name, step.Name, operand.OperandType));
    }

    private IGenericResult<object?> ResolveInputOperand(
        CalculationStepOperandConfiguration operand,
        CalculationStepConfiguration step,
        IReadOnlyList<ResolvedCalculationInput> inputs)
    {
        foreach (var input in inputs)
        {
            if (string.Equals(input.InputAlias, operand.InputAlias, StringComparison.Ordinal))
            {
                return ExtractField(input.ResolvedValue, operand, step);
            }
        }

        return GenericResult<object?>.Failure(
            CalculationStepExecutorLog.InputAliasUnresolved(
                _logger, operand.Name, step.Name, operand.InputAlias ?? string.Empty));
    }

    private IGenericResult<object?> ResolveStepOperand(
        CalculationStepOperandConfiguration operand,
        CalculationStepConfiguration step,
        IReadOnlyDictionary<string, object?> published)
    {
        if (operand.StepAlias is not null && published.TryGetValue(operand.StepAlias, out var value))
        {
            return ExtractField(value, operand, step);
        }

        return GenericResult<object?>.Failure(
            CalculationStepExecutorLog.StepAliasUnresolved(
                _logger, operand.Name, step.Name, operand.StepAlias ?? string.Empty));
    }

    /// <summary>
    /// Describes what an operand pointed at, for the trace.
    /// </summary>
    /// <remarks>
    /// Only ever called on the success path, after <see cref="ResolveOperand"/> has confirmed the
    /// operand type is one of the three known kinds and that the field it reads is populated — so
    /// the trailing empty string is unreachable rather than a default standing in for absent
    /// configuration.
    /// </remarks>
    private static string DescribeOperandSource(CalculationStepOperandConfiguration operand)
    {
        if (string.Equals(operand.OperandType, OperandTypeInput, StringComparison.Ordinal))
        {
            return operand.InputAlias ?? string.Empty;
        }

        if (string.Equals(operand.OperandType, OperandTypeStepReference, StringComparison.Ordinal))
        {
            return operand.StepAlias ?? string.Empty;
        }

        return string.Equals(operand.OperandType, OperandTypeLiteral, StringComparison.Ordinal)
            ? operand.LiteralValue ?? string.Empty
            : string.Empty;
    }

    /// <summary>
    /// Narrows a referenced value to one of its fields when the operand names one.
    /// </summary>
    private IGenericResult<object?> ExtractField(
        object? value,
        CalculationStepOperandConfiguration operand,
        CalculationStepConfiguration step)
    {
        if (string.IsNullOrEmpty(operand.FieldName))
        {
            return GenericResult<object?>.Success(value);
        }

        if (value is IReadOnlyDictionary<string, object?> row && row.TryGetValue(operand.FieldName!, out var field))
        {
            return GenericResult<object?>.Success(field);
        }

        if (value is IDictionary<string, object> mutableRow && mutableRow.TryGetValue(operand.FieldName!, out var mutableField))
        {
            return GenericResult<object?>.Success(mutableField);
        }

        return GenericResult<object?>.Failure(
            CalculationStepExecutorLog.FieldNotAddressable(
                _logger, operand.Name, step.Name, operand.FieldName!, value?.GetType().Name ?? "null"));
    }
}
