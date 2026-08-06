using System;
using System.Collections.Generic;
using System.Globalization;
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
/// Transform type that filters records based on an expression.
/// </summary>
[TypeOption(typeof(OptionTransformTypes), "Filter")]
public sealed class FilterTransformType : TransformTypeBase
{
    private static readonly string[] EqualsSeparator = ["=="];
    private static readonly string[] NotEqualsSeparator = ["!="];
    private static readonly string[] GreaterThanOrEqualSeparator = [">="];
    private static readonly string[] LessThanOrEqualSeparator = ["<="];
    private static readonly string[] GreaterThanSeparator = [">"];
    private static readonly string[] LessThanSeparator = ["<"];
    private static readonly string[] ContainsSeparator = [" contains ", " CONTAINS "];
    private static readonly string[] StartsWithSeparator = [" startswith ", " STARTSWITH "];
    private static readonly string[] EndsWithSeparator = [" endswith ", " ENDSWITH "];
    private static readonly string[] AndSeparator = [" && ", " AND ", " and "];
    private static readonly string[] OrSeparator = [" || ", " OR ", " or "];

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterTransformType"/> class.
    /// </summary>
    public FilterTransformType() : base(
        id: 2,
        name: "Filter",
        displayName: "Record Filter",
        description: "Filters records based on an expression - records not matching are excluded",
        category: "Filter",
        modifiesStructure: false,
        canFilterRecords: true)
    {
    }

    /// <inheritdoc />
    // Why: filter evaluation is pure in-memory predicate work (no I/O); Task.FromResult is honest
    // sync-returning-Task — the contract is async so future I/O-backed filters are first-class.
    public override Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, "Filter", configuration.GetType().Name)));
        }

        // Why: a Filter transform with no expression must fail loud, never silently pass every
        // record through — a param-less combine op is a configuration defect, not a no-op.
        if (string.IsNullOrWhiteSpace(config.FilterExpression))
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.FilterExpressionMissing(context.Logger, config.Name)));
        }

        var passes = EvaluateFilter(input, config.FilterExpression, context);

        if (passes)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Success(input));
        }

        // Return null to indicate record should be filtered out
        return Task.FromResult(GenericResult<IDictionary<string, object?>>.Success(null!));
    }

    /// <inheritdoc />
    // Why: filter evaluation is pure in-memory predicate work (no I/O); Task.FromResult is honest
    // sync-returning-Task — the contract is async so future I/O-backed filters are first-class.
    public override Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, "Filter", configuration.GetType().Name)));
        }

        // Why: a Filter transform with no expression must fail loud, never silently pass every
        // record through — a param-less combine op is a configuration defect, not a no-op.
        if (string.IsNullOrWhiteSpace(config.FilterExpression))
        {
            return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.FilterExpressionMissing(context.Logger, config.Name)));
        }

        var results = new List<IDictionary<string, object?>>();

        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (EvaluateFilter(input, config.FilterExpression, context))
            {
                results.Add(input);
            }
        }

        return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(results));
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (string.IsNullOrWhiteSpace(spec.FilterExpression))
        {
            return GenericResult.Failure(EtlLog.FilterExpressionMissing(logger, spec.Name));
        }

        config.FilterExpression = spec.FilterExpression;

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    private static bool EvaluateFilter(
        IDictionary<string, object?> record,
        string expression,
        ITransformContext context)
    {
        // Try to use IExpressionEvaluator from CalculationEngine if available
        if (context.CalculationEngine is IExpressionEvaluator evaluator)
        {
            var variables = new Dictionary<string, object?>(record, StringComparer.OrdinalIgnoreCase);
            var result = evaluator.EvaluatePredicate(expression, variables);
            if (result.IsSuccess)
            {
                return result.Value;
            }
            // Fall through to built-in evaluation if expression evaluator fails
        }

        // Built-in expression evaluation supporting common filter operations
        return EvaluateBuiltInFilter(record, expression.Trim());
    }

    private static bool EvaluateBuiltInFilter(IDictionary<string, object?> record, string expression)
    {
        if (TryEvaluateLogicalOperators(record, expression, out var logicalResult))
        {
            return logicalResult;
        }

        if (TryEvaluateNullChecks(record, expression, out var nullResult))
        {
            return nullResult;
        }

        if (TryEvaluateStringOperators(record, expression, out var stringResult))
        {
            return stringResult;
        }

        if (TryEvaluateComparisonOperators(record, expression, out var comparisonResult))
        {
            return comparisonResult;
        }

        // Boolean field check: just "FieldName" or "!FieldName"
        if (record.TryGetValue(expression, out var boolValue))
        {
            if (boolValue is bool b) return b;
            if (boolValue is string s) return bool.TryParse(s, out var parsed) && parsed;
            return boolValue != null;
        }

        // Default: pass through if expression cannot be parsed
        return true;
    }

    private static bool TryEvaluateLogicalOperators(IDictionary<string, object?> record, string expression, out bool result)
    {
        // OR operator: "Condition1 || Condition2"
        // Why: only match a separator at parenthesis depth 0 — an unqualified IndexOf would split
        // through a grouped sub-clause (e.g. "(Age>=18 && Active) || Name==Bob" would tear the "&&"
        // out of its parens and evaluate the wrong tree). Depth-tracking keeps grouped clauses intact.
        if (TryFindTopLevelSeparator(expression, OrSeparator, out var orIndex, out var orSepLength))
        {
            var left = expression[..orIndex].Trim();
            var right = expression[(orIndex + orSepLength)..].Trim();
            result = EvaluateBuiltInFilter(record, left) || EvaluateBuiltInFilter(record, right);
            return true;
        }

        // AND operator: "Condition1 && Condition2"
        if (TryFindTopLevelSeparator(expression, AndSeparator, out var andIndex, out var andSepLength))
        {
            var left = expression[..andIndex].Trim();
            var right = expression[(andIndex + andSepLength)..].Trim();
            result = EvaluateBuiltInFilter(record, left) && EvaluateBuiltInFilter(record, right);
            return true;
        }

        // Handle parenthesized expressions
        if (expression.StartsWith('(') && expression.EndsWith(')'))
        {
            result = EvaluateBuiltInFilter(record, expression[1..^1].Trim());
            return true;
        }

        // Handle NOT operator: "NOT Condition" or "!Condition"
        if (expression.StartsWith("NOT ", StringComparison.OrdinalIgnoreCase))
        {
            result = !EvaluateBuiltInFilter(record, expression[4..].Trim());
            return true;
        }
        if (expression.StartsWith('!') && !expression.StartsWith("!=", StringComparison.Ordinal))
        {
            result = !EvaluateBuiltInFilter(record, expression[1..].Trim());
            return true;
        }

        result = false;
        return false;
    }

    /// <summary>
    /// Finds the first occurrence (across the given separator spellings) of a logical-operator
    /// separator that sits at parenthesis depth 0, so callers never split inside a grouped "(...)"
    /// sub-clause.
    /// </summary>
    private static bool TryFindTopLevelSeparator(string expression, string[] separators, out int index, out int separatorLength)
    {
        foreach (var separator in separators)
        {
            var candidate = IndexOfAtDepthZero(expression, separator);
            if (candidate >= 0)
            {
                index = candidate;
                separatorLength = separator.Length;
                return true;
            }
        }

        index = -1;
        separatorLength = 0;
        return false;
    }

    private static int IndexOfAtDepthZero(string expression, string separator)
    {
        var depth = 0;
        var maxStart = expression.Length - separator.Length;
        for (var i = 0; i <= maxStart; i++)
        {
            var c = expression[i];
            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;
            }

            if (depth == 0 && expression.AsSpan(i, separator.Length).Equals(separator, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool TryEvaluateNullChecks(IDictionary<string, object?> record, string expression, out bool result)
    {
        if (expression.EndsWith("!= null", StringComparison.OrdinalIgnoreCase))
        {
            var fieldName = expression[..^7].Trim();
            result = record.TryGetValue(fieldName, out var value) && value != null;
            return true;
        }
        if (expression.EndsWith("== null", StringComparison.OrdinalIgnoreCase))
        {
            var fieldName = expression[..^7].Trim();
            result = !record.TryGetValue(fieldName, out var value) || value == null;
            return true;
        }

        result = false;
        return false;
    }

    private static bool TryEvaluateStringOperators(IDictionary<string, object?> record, string expression, out bool result)
    {
        if (TryEvaluateStringOperator(record, expression, ContainsSeparator, out result,
            (actual, search) => actual.ToString()?.Contains(search, StringComparison.OrdinalIgnoreCase) == true))
        {
            return true;
        }

        if (TryEvaluateStringOperator(record, expression, StartsWithSeparator, out result,
            (actual, search) => actual.ToString()?.StartsWith(search, StringComparison.OrdinalIgnoreCase) == true))
        {
            return true;
        }

        if (TryEvaluateStringOperator(record, expression, EndsWithSeparator, out result,
            (actual, search) => actual.ToString()?.EndsWith(search, StringComparison.OrdinalIgnoreCase) == true))
        {
            return true;
        }

        result = false;
        return false;
    }

    private static bool TryEvaluateStringOperator(
        IDictionary<string, object?> record,
        string expression,
        string[] separators,
        out bool result,
        Func<object, string, bool> comparison)
    {
        foreach (var sep in separators)
        {
            var index = expression.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var fieldName = expression[..index].Trim();
                var searchValue = ExtractValue(expression[(index + sep.Length)..].Trim());
                if (record.TryGetValue(fieldName, out var actualValue) && actualValue != null)
                {
                    result = comparison(actualValue, searchValue);
                    return true;
                }
                result = false;
                return true;
            }
        }

        result = false;
        return false;
    }

    private static bool TryEvaluateComparisonOperators(IDictionary<string, object?> record, string expression, out bool result)
    {
        // Order matters - check >= and <= before > and <
        if (TrySplitComparison(expression, GreaterThanOrEqualSeparator, out var geField, out var geValue))
        {
            result = CompareValues(record, geField, geValue, (a, b) => a >= b);
            return true;
        }

        if (TrySplitComparison(expression, LessThanOrEqualSeparator, out var leField, out var leValue))
        {
            result = CompareValues(record, leField, leValue, (a, b) => a <= b);
            return true;
        }

        if (TrySplitComparison(expression, NotEqualsSeparator, out var neField, out var neValue))
        {
            result = !CompareEqual(record, neField, neValue);
            return true;
        }

        if (TrySplitComparison(expression, EqualsSeparator, out var eqField, out var eqValue))
        {
            result = CompareEqual(record, eqField, eqValue);
            return true;
        }

        if (TrySplitComparison(expression, GreaterThanSeparator, out var gtField, out var gtValue))
        {
            result = CompareValues(record, gtField, gtValue, (a, b) => a > b);
            return true;
        }

        if (TrySplitComparison(expression, LessThanSeparator, out var ltField, out var ltValue))
        {
            result = CompareValues(record, ltField, ltValue, (a, b) => a < b);
            return true;
        }

        result = false;
        return false;
    }

    private static bool TrySplitComparison(string expression, string[] separators, out string field, out string value)
    {
        foreach (var sep in separators)
        {
            var index = expression.IndexOf(sep, StringComparison.Ordinal);
            if (index >= 0)
            {
                field = expression[..index].Trim();
                value = ExtractValue(expression[(index + sep.Length)..].Trim());
                return true;
            }
        }
        field = string.Empty;
        value = string.Empty;
        return false;
    }

    private static string ExtractValue(string value)
    {
        // Remove surrounding quotes
        if ((value.StartsWith('\'') && value.EndsWith('\'')) ||
            (value.StartsWith('"') && value.EndsWith('"')))
        {
            return value[1..^1];
        }
        return value;
    }

    private static bool CompareEqual(IDictionary<string, object?> record, string fieldName, string expectedValue)
    {
        if (!record.TryGetValue(fieldName, out var actualValue))
        {
            return false;
        }

        if (actualValue == null)
        {
            return string.IsNullOrEmpty(expectedValue);
        }

        return string.Equals(actualValue.ToString(), expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CompareValues(IDictionary<string, object?> record, string fieldName, string compareValue, Func<decimal, decimal, bool> comparison)
    {
        if (!record.TryGetValue(fieldName, out var actualValue) || actualValue == null)
        {
            return false;
        }

        // Try numeric comparison
        if (TryGetDecimal(actualValue, out var actualDecimal) &&
            decimal.TryParse(compareValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var compareDecimal))
        {
            return comparison(actualDecimal, compareDecimal);
        }

        // Fall back to string comparison
        var stringCompare = string.Compare(actualValue.ToString(), compareValue, StringComparison.OrdinalIgnoreCase);
        return comparison(stringCompare, 0);
    }

    private static bool TryGetDecimal(object? value, out decimal result)
    {
        result = 0;
        if (value == null) return false;

        return value switch
        {
            decimal d => (result = d) == d,
            double dbl => (result = (decimal)dbl) == (decimal)dbl,
            float f => (result = (decimal)f) == (decimal)f,
            int i => (result = i) == i,
            long l => (result = l) == l,
            short s => (result = s) == s,
            byte b => (result = b) == b,
            _ => decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result)
        };
    }
}
