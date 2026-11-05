using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Fdw.Operations.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Services.Quality.Thresholds;

/// <summary>The measures the quality threshold types take over the rows of a data set (FDW-3935), the rule logic that was in the quality rule evaluator.</summary>
public static class QualityMeasures
{
    /// <summary>Rows whose field is missing, null or blank.</summary>
    /// <param name="input">The parameters (FieldName) and rows.</param>
    /// <returns>rowCount and violations.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> NotNull(QualityMeasureInput input) =>
        Field(input, field => Measured(input, "violations", input.Rows.LongCount(row => IsBlank(row, field))));

    /// <summary>Rows that repeat a value another row has; null values are not counted.</summary>
    /// <param name="input">The parameters (FieldName) and rows.</param>
    /// <returns>rowCount and violations.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> Unique(QualityMeasureInput input) =>
        Field(input, field =>
        {
            var values = input.Rows.Where(row => row.TryGetValue(field, out var value) && value is not null).Select(row => row[field]?.ToString() ?? string.Empty).ToList();
            return Measured(input, "violations", (long)(values.Count - values.Distinct(StringComparer.Ordinal).Count()));
        });

    /// <summary>Rows whose field is missing, is not a number, or lies below the lowest or above the highest allowed value.</summary>
    /// <param name="input">The parameters (FieldName, MinValue, MaxValue) and rows.</param>
    /// <returns>rowCount and violations; a failure when neither limit is stated or a limit is not a number.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> InRange(QualityMeasureInput input) =>
        Field(input, field =>
        {
            var min = Limit(input, "MinValue");
            var max = Limit(input, "MaxValue");
            if (min.IsFailure || max.IsFailure)
            {
                return Failed(input, "MinValue and MaxValue, when stated, must be numbers");
            }

            if (min.Value is null && max.Value is null)
            {
                return Failed(input, "at least one of MinValue and MaxValue must be stated");
            }

            return Measured(input, "violations", input.Rows.LongCount(row => OutsideRange(row, field, min.Value, max.Value)));
        });

    /// <summary>Rows whose field is missing, blank or does not match the pattern; a match that times out counts as not matching.</summary>
    /// <param name="input">The parameters (FieldName, Pattern) and rows.</param>
    /// <returns>rowCount and violations; a failure when the pattern is not a valid expression.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> MatchesPattern(QualityMeasureInput input) =>
        Field(input, field =>
        {
            Regex regex;
            try
            {
                regex = new Regex((string)input.Parameters["Pattern"]!, RegexOptions.None, input.RegexTimeout);
            }
            catch (ArgumentException exception)
            {
                return Failed(input, "Pattern is not a valid regular expression: " + exception.Message);
            }

            return Measured(input, "violations", input.Rows.LongCount(row => !Matches(regex, row, field)));
        });

    /// <summary>Rows whose field is not one of the allowed values.</summary>
    /// <param name="input">The parameters (FieldName, AllowedValues) and rows.</param>
    /// <returns>rowCount and violations.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> InReferenceSet(QualityMeasureInput input) =>
        Field(input, field =>
        {
            var allowed = ((IEnumerable<object?>)input.Parameters["AllowedValues"]!).Select(value => value?.ToString()).ToHashSet(StringComparer.Ordinal);
            return Measured(input, "violations", input.Rows.LongCount(row => !(row.TryGetValue(field, out var value) && value?.ToString() is { } text && allowed.Contains(text))));
        });

    /// <summary>The number of distinct values the field holds; null values are not counted.</summary>
    /// <param name="input">The parameters (FieldName) and rows.</param>
    /// <returns>rowCount and distinctCount.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> DistinctCount(QualityMeasureInput input) =>
        Field(input, field => Measured(
            input, "distinctCount",
            input.Rows.Where(row => row.TryGetValue(field, out var value) && value is not null).Select(row => row[field]?.ToString() ?? string.Empty).Distinct(StringComparer.Ordinal).LongCount()));

    /// <summary>The number of rows the data set holds.</summary>
    /// <param name="input">The rows.</param>
    /// <returns>rowCount.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> RowCount(QualityMeasureInput input) =>
        GenericResult<IReadOnlyDictionary<string, object?>>.Success(new Dictionary<string, object?>(StringComparer.Ordinal) { ["rowCount"] = (long)input.Rows.Count });

    /// <summary>The share of rows whose field is missing, null or blank, 0 to 100; a data set with no rows has no share to measure.</summary>
    /// <param name="input">The parameters (FieldName) and rows.</param>
    /// <returns>rowCount and nullPercentage; a failure for no rows.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> NullPercentage(QualityMeasureInput input) =>
        Field(input, field => input.Rows.Count == 0
            ? Failed(input, "the data set has no rows, so there is no share to measure")
            : Measured(input, "nullPercentage", 100d * input.Rows.Count(row => IsBlank(row, field)) / input.Rows.Count));

    /// <summary>A custom expression has no evaluator yet, as in the quality rule evaluator it replaces; the rule is reported not run.</summary>
    /// <param name="input">The parameters and rows.</param>
    /// <returns>A failure saying the expression kind is not supported.</returns>
    public static IGenericResult<IReadOnlyDictionary<string, object?>> CustomExpression(QualityMeasureInput input) =>
        Failed(input, "CustomExpression rules are not supported yet");

    private static IGenericResult<IReadOnlyDictionary<string, object?>> Field(QualityMeasureInput input, Func<string, IGenericResult<IReadOnlyDictionary<string, object?>>> take) =>
        input.Parameters.TryGetValue("FieldName", out var field) && field is string { Length: > 0 } name
            ? take(name)
            : Failed(input, "FieldName is required");

    private static IGenericResult<IReadOnlyDictionary<string, object?>> Measured(QualityMeasureInput input, string measure, object value) =>
        GenericResult<IReadOnlyDictionary<string, object?>>.Success(
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["rowCount"] = (long)input.Rows.Count, [measure] = value });

    private static IGenericResult<IReadOnlyDictionary<string, object?>> Failed(QualityMeasureInput input, string reason) =>
        GenericResult.Failure(
            OperationsResultCodes.ByName("QualityMeasureFailed"), ResultDetails.Create().With("Threshold", input.ThresholdName).With("Reason", reason))
            .ToNewResult<IReadOnlyDictionary<string, object?>>();

    private static bool IsBlank(IReadOnlyDictionary<string, object?> row, string field) =>
        !row.TryGetValue(field, out var value) || value is null || (value is string text && string.IsNullOrWhiteSpace(text));

    private static IGenericResult<double?> Limit(QualityMeasureInput input, string name)
    {
        if (!input.Parameters.TryGetValue(name, out var stated) || stated is null)
        {
            return GenericResult<double?>.Success(null);
        }

        return double.TryParse(stated.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? GenericResult<double?>.Success(number)
            : GenericResult.Failure(OperationsResultCodes.ByName("QualityMeasureFailed"), ResultDetails.Create().With("Threshold", input.ThresholdName).With("Reason", name + " is not a number")).ToNewResult<double?>();
    }

    private static bool OutsideRange(IReadOnlyDictionary<string, object?> row, string field, double? min, double? max) =>
        !(row.TryGetValue(field, out var value) && value is not null && double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        || (min is { } low && number < low) || (max is { } high && number > high);

    private static bool Matches(Regex regex, IReadOnlyDictionary<string, object?> row, string field)
    {
        if (!row.TryGetValue(field, out var value) || value?.ToString() is not { Length: > 0 } text)
        {
            return false;
        }

        try
        {
            return regex.IsMatch(text);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
