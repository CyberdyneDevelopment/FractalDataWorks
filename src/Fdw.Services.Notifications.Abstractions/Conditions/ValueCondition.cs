using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Fires based on field value comparison.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationConditionTypes), "ValueCondition", RestrictToCurrentCompilation = true)]
public sealed class ValueCondition : NotificationConditionTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueCondition"/> class.
    /// </summary>
    public ValueCondition()
        : base(5, "ValueCondition", "compare", "Primary")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<bool> Evaluate(NotificationContext context)
    {
        if (string.IsNullOrEmpty(context.Operator) || context.Value is null || context.ActualValue is null)
        {
            return GenericResult<bool>.Success(false);
        }

        var result = EvaluateOperator(context.ActualValue, context.Operator!, context.Value);
        return GenericResult<bool>.Success(context.IsNegated ? !result : result);
    }

    private static bool EvaluateOperator(string actual, string op, string expected)
    {
        return op switch
        {
            "Equal" => string.Equals(actual, expected, StringComparison.Ordinal),
            "NotEqual" => !string.Equals(actual, expected, StringComparison.Ordinal),
            "Contains" => actual.Contains(expected),
            "StartsWith" => actual.StartsWith(expected, StringComparison.Ordinal),
            "EndsWith" => actual.EndsWith(expected, StringComparison.Ordinal),
            "GreaterThan" => CompareNumeric(actual, expected) > 0,
            "LessThan" => CompareNumeric(actual, expected) < 0,
            "GreaterThanOrEqual" => CompareNumeric(actual, expected) >= 0,
            "LessThanOrEqual" => CompareNumeric(actual, expected) <= 0,
            _ => false
        };
    }

    private static int CompareNumeric(string a, string b)
    {
        if (double.TryParse(a, NumberStyles.Any, CultureInfo.InvariantCulture, out var aVal) &&
            double.TryParse(b, NumberStyles.Any, CultureInfo.InvariantCulture, out var bVal))
        {
            return aVal.CompareTo(bVal);
        }

        return string.Compare(a, b, StringComparison.Ordinal);
    }
}
