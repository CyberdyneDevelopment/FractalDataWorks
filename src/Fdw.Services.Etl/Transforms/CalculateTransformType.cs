using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Transform type that calculates new fields based on expressions.
/// </summary>
/// <remarks>
/// Why: reads the typed <see cref="PipelineTransformConfiguration.Calculations"/> cascade children
/// (ordered by <c>ExecutionOrder</c>) instead of the deleted <c>ConfigurationJson</c> blob. A calc's
/// <c>FormulaLanguage</c> is resolved against <see cref="FormulaLanguages"/>; non-Builtin languages
/// require an <see cref="IExpressionEvaluator"/> on <see cref="ITransformContext.CalculationEngine"/>
/// and fail loud when absent — no silent pass-through.
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "Calculate")]
public sealed class CalculateTransformType : TransformTypeBase
{
    private static readonly string[] AdditionSeparator = [" + "];
    private static readonly string[] MultiplicationSeparator = [" * "];
    private static readonly string[] DivisionSeparator = [" / "];
    private static readonly string[] SubtractionSeparator = [" - "];

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculateTransformType"/> class.
    /// </summary>
    public CalculateTransformType() : base(
        id: 4,
        name: "Calculate",
        displayName: "Calculated Field",
        description: "Creates new fields by evaluating expressions or formulas",
        category: "Transform",
        modifiesStructure: true,
        canFilterRecords: false)
    {
    }

    /// <inheritdoc />
    // Why: expression evaluation is pure in-memory arithmetic/string work (no I/O); Task.FromResult is
    // honest sync-returning-Task — the contract is async so future I/O-backed calculators are first-class.
    public override Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, "Calculate", configuration.GetType().Name)));
        }

        if (config.Calculations.Count == 0)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.CalculationParamsMissing(context.Logger, config.Name)));
        }

        var output = new Dictionary<string, object?>(input, StringComparer.OrdinalIgnoreCase);

        foreach (var calc in config.Calculations.OrderBy(c => c.ExecutionOrder))
        {
            var languageResult = ResolveLanguage(calc.FormulaLanguage, config.Name, context.Logger, context.CalculationEngine);
            if (!languageResult.IsSuccess)
            {
                return Task.FromResult(languageResult.ToNewResult<IDictionary<string, object?>>());
            }

            try
            {
                output[calc.OutputField] = languageResult.Value!.IsBuiltin
                    ? EvaluateExpression(calc.Expression, output, context)
                    : EvaluateWithEngine(calc.Expression, output, (IExpressionEvaluator)context.CalculationEngine!);
            }
            catch (Exception ex)
            {
                return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                    EtlLog.CalculationFailed(context.Logger, ex, calc.OutputField)));
            }
        }

        return Task.FromResult(GenericResult<IDictionary<string, object?>>.Success(output));
    }

    /// <inheritdoc />
    // Why: structural preconditions (config type, non-empty Calculations, resolvable FormulaLanguage,
    // an available engine for non-Builtin languages) are checked ONCE up front and fail the whole step
    // loudly — a per-record expression-evaluation exception is a data problem, not a config problem, so
    // it is reported via context.ReportError and that single record is excluded from the output set.
    public override async Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, "Calculate", configuration.GetType().Name));
        }

        if (config.Calculations.Count == 0)
        {
            return GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.CalculationParamsMissing(context.Logger, config.Name));
        }

        foreach (var calc in config.Calculations)
        {
            var languageResult = ResolveLanguage(calc.FormulaLanguage, config.Name, context.Logger, context.CalculationEngine);
            if (!languageResult.IsSuccess)
            {
                return languageResult.ToNewResult<IEnumerable<IDictionary<string, object?>>>();
            }
        }

        var results = new List<IDictionary<string, object?>>();
        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var transformResult = await Transform(input, config, context, cancellationToken).ConfigureAwait(false);
            if (!transformResult.IsSuccess)
            {
                context.ReportError(transformResult.CurrentMessage ?? "Calculation failed", input);
                continue;
            }

            results.Add(transformResult.Value!);
        }

        return GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(results);
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (spec.ComputedColumns.Count == 0)
        {
            return GenericResult.Failure(EtlLog.CalculationParamsMissing(logger, spec.Name));
        }

        foreach (var calc in spec.ComputedColumns)
        {
            if (FormulaLanguages.ByName(calc.FormulaLanguage) == FormulaLanguages.NotFound)
            {
                return GenericResult.Failure(EtlLog.UnknownFormulaLanguage(logger, calc.FormulaLanguage, spec.Name));
            }
        }

        config.Calculations = spec.ComputedColumns
            .Select((calc, executionOrder) => new PipelineTransformCalculationConfiguration
            {
                PipelineTransformId = config.Id,
                Name = calc.OutputField,
                OutputField = calc.OutputField,
                Expression = calc.Formula,
                FormulaLanguage = calc.FormulaLanguage,
                ExecutionOrder = executionOrder
            })
            .ToList();

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    /// <summary>
    /// Resolves a calculation's <c>FormulaLanguage</c> against <see cref="FormulaLanguages"/> and, for a
    /// non-Builtin language, requires an <see cref="IExpressionEvaluator"/> engine — fails loud otherwise.
    /// </summary>
    private static IGenericResult<IFormulaLanguage> ResolveLanguage(
        string languageName, string transformName, ILogger logger, object? calculationEngine)
    {
        var language = FormulaLanguages.ByName(languageName);
        if (language == FormulaLanguages.NotFound)
        {
            return GenericResult<IFormulaLanguage>.Failure(EtlLog.UnknownFormulaLanguage(logger, languageName, transformName));
        }

        if (!language.IsBuiltin && calculationEngine is not IExpressionEvaluator)
        {
            return GenericResult<IFormulaLanguage>.Failure(EtlLog.FormulaEngineUnavailable(logger, languageName));
        }

        return GenericResult<IFormulaLanguage>.Success(language);
    }

    private static object? EvaluateWithEngine(string expression, Dictionary<string, object?> record, IExpressionEvaluator evaluator)
    {
        var variables = new Dictionary<string, object?>(record, StringComparer.OrdinalIgnoreCase);
        var result = evaluator.Evaluate<object?>(expression, variables);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.CurrentMessage ?? "Expression evaluation failed");
        }

        return result.Value;
    }

    /// <summary>
    /// Evaluates a simple expression against a record.
    /// Supports basic arithmetic (+, -, *, /) and field references.
    /// For complex expressions, use the CalculationEngine from context.
    /// </summary>
    private static object? EvaluateExpression(
        string expression,
        Dictionary<string, object?> record,
        ITransformContext context)
    {
        // Concatenation: "Field1 + ' ' + Field2"
        if (expression.Contains(" + ", StringComparison.Ordinal))
        {
            return EvaluateConcatenation(expression, record);
        }

        // Arithmetic operations: *, /, -
        if (TryEvaluateArithmetic(expression, MultiplicationSeparator, record, (a, b) => a * b, out var mulResult))
        {
            return mulResult;
        }

        if (TryEvaluateArithmetic(expression, DivisionSeparator, record, (a, b) => b != 0 ? a / b : (decimal?)null, out var divResult))
        {
            return divResult;
        }

        if (TryEvaluateArithmetic(expression, SubtractionSeparator, record, (a, b) => a - b, out var subResult))
        {
            return subResult;
        }

        return EvaluateLiteralOrFieldReference(expression, record);
    }

    private static string? EvaluateConcatenation(string expression, Dictionary<string, object?> record)
    {
        var parts = expression.Split(AdditionSeparator, StringSplitOptions.None);
        var result = "";
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith('\'') && trimmed.EndsWith('\''))
            {
                result += trimmed.Trim('\'');
            }
            else if (record.TryGetValue(trimmed, out var value))
            {
                result += value?.ToString() ?? "";
            }
        }
        return result;
    }

    private static bool TryEvaluateArithmetic(
        string expression,
        string[] separator,
        Dictionary<string, object?> record,
        Func<decimal, decimal, decimal?> operation,
        out object? result)
    {
        result = null;
        var separatorString = separator[0];
        if (!expression.Contains(separatorString, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = expression.Split(separator, StringSplitOptions.None);
        if (parts.Length != 2)
        {
            return false;
        }

        var left = GetNumericValue(parts[0].Trim(), record);
        var right = GetNumericValue(parts[1].Trim(), record);
        if (left.HasValue && right.HasValue)
        {
            result = operation(left.Value, right.Value);
            return result != null;
        }

        return false;
    }

    private static object? EvaluateLiteralOrFieldReference(string expression, Dictionary<string, object?> record)
    {
        // Simple field reference
        if (record.TryGetValue(expression.Trim(), out var fieldValue))
        {
            return fieldValue;
        }

        // Literal value
        if (decimal.TryParse(expression, CultureInfo.InvariantCulture, out var numericLiteral))
        {
            return numericLiteral;
        }

        if (expression.StartsWith('\'') && expression.EndsWith('\''))
        {
            return expression.Trim('\'');
        }

        return null;
    }

    private static decimal? GetNumericValue(string fieldOrValue, Dictionary<string, object?> record)
    {
        if (decimal.TryParse(fieldOrValue, CultureInfo.InvariantCulture, out var literal))
        {
            return literal;
        }

        if (record.TryGetValue(fieldOrValue, out var value) && value != null)
        {
            if (value is decimal d) return d;
            if (value is double dbl) return (decimal)dbl;
            if (value is float f) return (decimal)f;
            if (value is int i) return i;
            if (value is long l) return l;
            if (decimal.TryParse(value.ToString(), CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}
