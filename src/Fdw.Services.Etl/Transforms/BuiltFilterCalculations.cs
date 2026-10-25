using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Compilation;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Evaluates a Built transformation for a query filter: the calculation a filter names for its value, run with the values the filter
/// supplies for its inputs, in process.
/// </summary>
/// <remarks>
/// The calculation is compiled by the same engine and against the same limit settings as a Compute stage, and may yield a date-time,
/// which a Compute stage may not. The current time comes from the <see cref="TimeProvider"/> the host registered, once per
/// evaluation. Every input the calculation declares needs a value and every value needs a declared input; a missing one fails the
/// evaluation naming the calculation and the input, and nothing is assumed.
/// </remarks>
public sealed class BuiltFilterCalculations : IFilterCalculations
{
    // The value type an input declares, and the CLR type a filter's value for it must have.
    private static readonly Dictionary<string, Type> InputTypes = new(StringComparer.Ordinal)
    {
        ["Number"] = typeof(decimal),
        ["Flag"] = typeof(bool),
    };

    private readonly ITransformationConfigurationProvider _transformations;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuiltFilterCalculations"/> class.
    /// </summary>
    /// <param name="transformations">Finds the calculation by name.</param>
    /// <param name="settings">Supplies the limits on a calculation.</param>
    /// <param name="clock">The source of the current time.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    public BuiltFilterCalculations(
        ITransformationConfigurationProvider transformations,
        IEffectiveSettingsProvider settings,
        TimeProvider clock,
        ILogger<BuiltFilterCalculations>? logger = null)
    {
        _transformations = transformations ?? throw new ArgumentNullException(nameof(transformations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? NullLogger<BuiltFilterCalculations>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<FilterCalculationValue>> Evaluate(
        string calculation, IReadOnlyDictionary<string, object?> inputs, CancellationToken cancellationToken = default)
    {
        var built = await Find(calculation, cancellationToken).ConfigureAwait(false);
        if (built.IsFailure)
        {
            return built.ToNewResult<FilterCalculationValue>();
        }

        var record = Record(calculation, built.Value!, inputs);
        if (record.IsFailure)
        {
            return record.ToNewResult<FilterCalculationValue>();
        }

        var compiled = Compile(calculation, built.Value!);
        if (compiled.IsFailure)
        {
            return compiled.ToNewResult<FilterCalculationValue>();
        }

        var state = new CalculationRunState { Clock = _clock.GetUtcNow };
        var results = new decimal[compiled.Value!.EmittedNames.Count];
        if (!compiled.Value!.TryEvaluate(record.Value!, results, state))
        {
            FilterCalculationLog.CalculationFailed(_logger, calculation);
            return compiled.Value!.Describe(state).ToNewResult<FilterCalculationValue>();
        }

        FilterCalculationLog.Evaluated(_logger, calculation, inputs.Count);
        var kind = compiled.Value!.EmittedKinds[0];
        return GenericResult<FilterCalculationValue>.Success(new FilterCalculationValue(kind.ClrType, kind.Read(results[0])));
    }

    private async Task<IGenericResult<BuiltTransformationConfiguration>> Find(string calculation, CancellationToken cancellationToken)
    {
        var found = await _transformations.Get(calculation, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
        {
            return GenericResult<BuiltTransformationConfiguration>.Failure(FilterCalculationLog.CalculationNotFound(_logger, calculation));
        }

        if (found.Value is not BuiltTransformationConfiguration built)
        {
            return GenericResult<BuiltTransformationConfiguration>.Failure(
                FilterCalculationLog.NotASingleValue(_logger, calculation, found.Value.Implementation, "records"));
        }

        return built.TableOutputs.Count > 0
            ? GenericResult<BuiltTransformationConfiguration>.Failure(FilterCalculationLog.NotASingleValue(_logger, calculation, built.Implementation, "tables"))
            : GenericResult<BuiltTransformationConfiguration>.Success(built);
    }

    // The record the calculation reads: one value per declared input, each of the type the input declares.
    private IGenericResult<IReadOnlyDictionary<string, object?>> Record(
        string calculation, BuiltTransformationConfiguration built, IReadOnlyDictionary<string, object?> inputs)
    {
        foreach (var supplied in inputs.Keys)
        {
            if (!built.Inputs.Any(declared => string.Equals(declared.Name, supplied, StringComparison.Ordinal)))
            {
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(FilterCalculationLog.InputNotDeclared(_logger, calculation, supplied));
            }
        }

        var record = new Dictionary<string, object?>(built.Inputs.Count, StringComparer.Ordinal);
        foreach (var declared in built.Inputs)
        {
            if (!inputs.TryGetValue(declared.Name, out var value) || value is null)
            {
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(FilterCalculationLog.InputNotSupplied(_logger, calculation, declared.Name));
            }

            if (!InputTypes.TryGetValue(declared.ValueType, out var expected) || value.GetType() != expected)
            {
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(
                    FilterCalculationLog.InputWrongType(_logger, calculation, declared.Name, declared.ValueType, value.GetType().Name));
            }

            record[declared.Name] = value;
        }

        return GenericResult<IReadOnlyDictionary<string, object?>>.Success(record);
    }

    private IGenericResult<CompiledCalculation> Compile(string calculation, BuiltTransformationConfiguration built)
    {
        var limits = CalculationLimitSettings.Read(_settings, _logger);
        if (limits.IsFailure)
        {
            return limits.ToNewResult<CompiledCalculation>();
        }

        var array = BuiltTransformationConverter.ToArray(built, limits.Value!);
        if (array.IsFailure)
        {
            return array.ToNewResult<CompiledCalculation>();
        }

        var kept = array.Value!.Results.Count(result => result.Emit);
        if (kept != 1)
        {
            return GenericResult<CompiledCalculation>.Failure(FilterCalculationLog.NotOneKeptResult(_logger, calculation, kept));
        }

        var compiled = new CalculationCompiler().Compile(array.Value!, limits.Value!, allowDateTimeResults: true);
        if (compiled.IsFailure)
        {
            FilterCalculationLog.CalculationRefused(_logger, calculation);
        }

        return compiled;
    }
}
