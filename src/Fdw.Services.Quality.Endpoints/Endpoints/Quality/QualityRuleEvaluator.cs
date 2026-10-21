using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>
/// Evaluates a quality rule over the rows of a data set. The one place a rule is judged: the execute-one and execute-all
/// endpoints both call it, so a verdict is worked the same way whichever route asked for it (FDW-1363).
/// </summary>
public static class QualityRuleEvaluator
{
    /// <summary>Evaluates one quality rule over the rows of its data set and answers its verdict.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="data">The rows the rule is evaluated over.</param>
    /// <param name="totalCount">The number of rows.</param>
    /// <returns>The verdict: Passed or Failed with the number of failing rows, or NotSupported or Failed with the reason the rule could not run.</returns>
    public static QualityCheckResultResponse Evaluate(IQualityRuleImplementationConfiguration rule, IReadOnlyList<Dictionary<string, object>> data, int totalCount)
    {
        return rule.RuleType switch
        {
            "NotNull" => ValidateNotNull(data, rule.FieldName, totalCount),
            "Unique" => ValidateUnique(data, rule.FieldName, totalCount),
            "InRange" => ValidateInRange(data, rule.FieldName, rule.MinValue, rule.MaxValue, totalCount),
            "MatchesPattern" => ValidatePattern(data, rule.FieldName, rule.Pattern, totalCount),
            "CustomExpression" => new QualityCheckResultResponse
            {
                Status = "NotSupported",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "CustomExpression rules are not yet supported"
            },
            _ => new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = $"Unknown rule type: {rule.RuleType}"
            }
        };
    }

    /// <summary>Validates that the specified field is not null or empty in all rows.</summary>
    private static QualityCheckResultResponse ValidateNotNull(IReadOnlyList<Dictionary<string, object>> data, string? fieldName, int totalCount)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "FieldName is required for NotNull rule"
            };
        }

        var failureCount = data.Count(row =>
            !row.ContainsKey(fieldName) ||
            row[fieldName] == null ||
            (row[fieldName] is string str && string.IsNullOrWhiteSpace(str)));

        return new QualityCheckResultResponse
        {
            Status = failureCount == 0 ? "Passed" : "Failed",
            FailureCount = failureCount,
            TotalCount = totalCount
        };
    }

    /// <summary>Validates that the specified field contains unique values across all rows.</summary>
    private static QualityCheckResultResponse ValidateUnique(IReadOnlyList<Dictionary<string, object>> data, string? fieldName, int totalCount)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "FieldName is required for Unique rule"
            };
        }

        var values = data
            .Where(row => row.ContainsKey(fieldName) && row[fieldName] != null)
            .Select(row => row[fieldName]?.ToString() ?? string.Empty)
            .ToList();

        var uniqueValues = values.Distinct(StringComparer.Ordinal).Count();
        var failureCount = values.Count - uniqueValues;

        return new QualityCheckResultResponse
        {
            Status = failureCount == 0 ? "Passed" : "Failed",
            FailureCount = failureCount,
            TotalCount = totalCount
        };
    }

    /// <summary>Validates that the specified field values fall within the given numeric range.</summary>
    private static QualityCheckResultResponse ValidateInRange(
        IReadOnlyList<Dictionary<string, object>> data,
        string? fieldName,
        string? minValue,
        string? maxValue,
        int totalCount)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "FieldName is required for InRange rule"
            };
        }

        if (!double.TryParse(minValue, CultureInfo.InvariantCulture, out var min) || !double.TryParse(maxValue, CultureInfo.InvariantCulture, out var max))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "MinValue and MaxValue must be numeric"
            };
        }

        var failureCount = data.Count(row =>
        {
            if (!row.TryGetValue(fieldName, out var fieldValue) || fieldValue == null)
                return true;

            var valueStr = fieldValue.ToString();
            if (!double.TryParse(valueStr, CultureInfo.InvariantCulture, out var value))
                return true;

            return value < min || value > max;
        });

        return new QualityCheckResultResponse
        {
            Status = failureCount == 0 ? "Passed" : "Failed",
            FailureCount = failureCount,
            TotalCount = totalCount
        };
    }

    /// <summary>Validates that the specified field values match the given regex pattern.</summary>
    private static QualityCheckResultResponse ValidatePattern(
        IReadOnlyList<Dictionary<string, object>> data,
        string? fieldName,
        string? pattern,
        int totalCount)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "FieldName is required for MatchesPattern rule"
            };
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = "Pattern is required for MatchesPattern rule"
            };
        }

        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
        }
        catch (Exception ex)
        {
            return new QualityCheckResultResponse
            {
                Status = "Failed",
                FailureCount = 0,
                TotalCount = totalCount,
                ErrorMessage = $"Invalid regex pattern: {ex.Message}"
            };
        }

        var failureCount = data.Count(row =>
        {
            if (!row.TryGetValue(fieldName, out var fieldValue) || fieldValue == null)
                return true;

            var valueStr = fieldValue.ToString();
            if (string.IsNullOrEmpty(valueStr))
                return true;

            try
            {
                return !regex.IsMatch(valueStr);
            }
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException ex)
            {
                _ = ex;
                return true;
            }
        });

        return new QualityCheckResultResponse
        {
            Status = failureCount == 0 ? "Passed" : "Failed",
            FailureCount = failureCount,
            TotalCount = totalCount
        };
    }
}
