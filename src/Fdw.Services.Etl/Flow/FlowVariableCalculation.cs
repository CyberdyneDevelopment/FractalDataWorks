using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.Transformations;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The calculation a Set variable step may carry (FDW-1957, FDW-2042): one named field transformation applied to a value of the run, how a
/// file name is worked out from a date parameter. The transformation is found by name in <see cref="TransformationTypes"/>, as a field
/// mapping finds its transformers, so nothing is written twice. Several Set variable steps in a row chain several transformations.
/// </summary>
public sealed class FlowVariableCalculation
{
    private readonly Func<FlowVariables, object?> _input;
    private readonly string _transformation;
    private readonly IReadOnlyList<FlowStepParameter> _parameters;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="FlowVariableCalculation"/> class.</summary>
    /// <param name="input">Reads the value to transform from the variables in scope.</param>
    /// <param name="transformation">The name of the field transformation to apply.</param>
    /// <param name="parameters">The transformation's parameters, as text; a value may name variables in double braces.</param>
    /// <param name="logger">The logger.</param>
    public FlowVariableCalculation(
        Func<FlowVariables, object?> input,
        string transformation,
        IReadOnlyList<FlowStepParameter> parameters,
        ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transformation);
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _transformation = transformation;
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Works the value out.</summary>
    /// <param name="step">The name of the step that asks, for the messages.</param>
    /// <param name="variables">The variables in scope.</param>
    /// <param name="cancellationToken">Cancels the transformation.</param>
    /// <returns>The value, or the failure that says why it could not be worked out.</returns>
    [ConventionOverride(MaxMethodLines = 70, MaxCyclomaticComplexity = 15)]  // One sequence of checks that each end the step with its reason, then the transformation
    public async Task<IGenericResult<object?>> Work(string step, FlowVariables variables, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(variables);

        if (TransformationTypes.ByName(_transformation) is not FieldTransformationBase transformer)
        {
            return GenericResult<object?>.Failure(FlowLog.CalculateTransformationUnknown(_logger, step, _transformation));
        }

        Dictionary<string, string> parameters;
        object? input;
        try
        {
            parameters = _parameters.ToDictionary(parameter => parameter.Name, parameter => parameter.Value(variables), StringComparer.Ordinal);
            input = _input(variables);
        }
        catch (InvalidOperationException ex) when (ex is not null)
        {
            return GenericResult<object?>.Failure(FlowLog.CalculateInputUnreadable(_logger, step, ex.Message));
        }

        var missing = transformer.ExpectedParameters
            .Where(definition => definition.IsRequired && !parameters.ContainsKey(definition.Name))
            .Select(definition => definition.Name)
            .ToList();
        if (missing.Count > 0)
        {
            return GenericResult<object?>.Failure(FlowLog.CalculateParametersMissing(_logger, step, _transformation, string.Join(", ", missing)));
        }

        var now = DateTimeOffset.UtcNow;
        var transformContext = new TransformationContext
        {
            OperatingDate = DateOnly.FromDateTime(now.UtcDateTime),
            ExecutionTimestamp = now,
            Parameters = parameters,
            CancellationToken = cancellationToken,
        };

        // Why a catch: a transformer states a value it cannot take (a text where it needs a date) by throwing, so that is a failed step, not a crash.
        IGenericResult<object?> transformed;
        try
        {
            transformed = await transformer.Transform(input, transformContext, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex is not null)
        {
            return GenericResult<object?>.Failure(FlowLog.CalculateFailed(_logger, step, _transformation, ex.Message));
        }

        return transformed.IsFailure
            ? GenericResult<object?>.Failure(FlowLog.CalculateFailed(_logger, step, _transformation, transformed.CurrentMessage))
            : GenericResult<object?>.Success(transformed.Value);
    }
}
