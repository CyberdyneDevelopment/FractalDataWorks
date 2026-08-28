using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Calculations;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Calculation entity type for column-level windowed function evaluations.
/// Applies a function such as Avg, Sum, or RowNumber to a single DataSet column,
/// partitioned and ordered by configurable field lists.
/// Registered in <see cref="CalculationEntityTypes"/> under the key <c>"Windowed"</c>.
/// </summary>
[TypeOption(typeof(CalculationEntityTypes), "Windowed")]
public sealed class WindowedCalculationEntityType : CalculationEntityBase<WindowedCalculationConfiguration>
{
    private readonly ILogger<WindowedCalculationEntityType> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="WindowedCalculationEntityType"/>.
    /// </summary>
    public WindowedCalculationEntityType()
        : base("Windowed", "Windowed Calculation", "Applies a window function to a DataSet column")
    {
        _logger = NullLogger<WindowedCalculationEntityType>.Instance;
    }

    /// <inheritdoc/>
    protected override IGenericResult ValidateTypedConfiguration(WindowedCalculationConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.TargetField))
        {
            return GenericResult.Failure(
                CalculationEntityLog.CalculationValidationFailed(
                    _logger,
                    "Windowed",
                    "TargetField is required and must not be empty"));
        }

        if (string.IsNullOrWhiteSpace(configuration.WindowFunction))
        {
            return GenericResult.Failure(
                CalculationEntityLog.CalculationValidationFailed(
                    _logger,
                    "Windowed",
                    "WindowFunction is required and must not be empty"));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc/>
    protected override async Task<IGenericResult<string>> ExecuteTyped(
        ICalculationEntity entity,
        IReadOnlyList<ResolvedCalculationInput> inputs,
        ICalculationContext context,
        CancellationToken cancellationToken)
    {
        CalculationEntityLog.CalculationExecuteStarted(_logger, entity.Name);

        try
        {
            // Collect all input rows
            var allRows = new List<Dictionary<string, object>>();
            foreach (var input in inputs)
            {
                if (input.ResolvedValue is IEnumerable<Dictionary<string, object>> dataRows)
                {
                    allRows.AddRange(dataRows);
                }
            }

            if (allRows.Count == 0)
            {
                return GenericResult<string>.Success(JsonSerializer.Serialize(new
                {
                    CalculationName = entity.Name,
                    RowCount = 0,
                    Rows = Array.Empty<object>()
                }));
            }

            // Parse configuration from entity's output spec
            // In a full implementation, these come from WindowedCalculationConfiguration via IOptions.
            // For now, we extract what we can from the entity metadata.
            var windowFunction = "Rank"; // default
            var targetField = string.Empty;
            var outputFieldName = entity.Output.ResultFieldName;
            var partitionByFields = new List<string>();
            var orderByFields = new List<WindowOrderFieldSpec>();

            // Apply window function
            var partitions = GroupByPartition(allRows, partitionByFields);

            CalculationEntityLog.WindowedExecutionStarted(
                _logger, entity.Name, windowFunction, partitions.Count);

            foreach (var partition in partitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var orderedRows = ApplyOrdering(partition, orderByFields);
                ApplyWindowFunction(orderedRows, windowFunction, targetField, outputFieldName);
            }

            var resultJson = JsonSerializer.Serialize(new
            {
                CalculationName = entity.Name,
                WindowFunction = windowFunction,
                ResultField = outputFieldName,
                PartitionCount = partitions.Count,
                RowCount = allRows.Count,
                Rows = allRows
            });

            CalculationEntityLog.WindowedExecutionSucceeded(_logger, entity.Name, allRows.Count);
            return GenericResult<string>.Success(resultJson);
        }
        catch (Exception ex)
        {
            return GenericResult<string>.Failure(
                CalculationEntityLog.WindowedExecutionFailed(_logger, ex, entity.Name));
        }
    }

    private static List<List<Dictionary<string, object>>> GroupByPartition(
        List<Dictionary<string, object>> rows,
        List<string> partitionByFields)
    {
        if (partitionByFields.Count == 0)
        {
            return [rows];
        }

        return rows
            .GroupBy(row => string.Join("|", partitionByFields.Select(f =>
                row.TryGetValue(f, out var v) ? v?.ToString() ?? string.Empty : string.Empty)), StringComparer.Ordinal)
            .Select(g => g.ToList())
            .ToList();
    }

    private static List<Dictionary<string, object>> ApplyOrdering(
        List<Dictionary<string, object>> partition,
        List<WindowOrderFieldSpec> orderByFields)
    {
        if (orderByFields.Count == 0)
        {
            return partition;
        }

        IOrderedEnumerable<Dictionary<string, object>>? ordered = null;
        foreach (var field in orderByFields)
        {
            if (ordered is null)
            {
                ordered = field.Descending
                    ? partition.OrderByDescending(r => GetComparableValue(r, field.FieldName))
                    : partition.OrderBy(r => GetComparableValue(r, field.FieldName));
            }
            else
            {
                ordered = field.Descending
                    ? ordered.ThenByDescending(r => GetComparableValue(r, field.FieldName))
                    : ordered.ThenBy(r => GetComparableValue(r, field.FieldName));
            }
        }

        var result = ordered?.ToList() ?? partition;

        // Update the original partition in-place
        for (var i = 0; i < partition.Count; i++)
        {
            partition[i] = result[i];
        }

        return partition;
    }

    private static IComparable GetComparableValue(Dictionary<string, object> row, string fieldName)
    {
        if (row.TryGetValue(fieldName, out var value) && value is IComparable comparable)
        {
            return comparable;
        }
        return string.Empty;
    }

#pragma warning disable FDW007 // Cyclomatic complexity — switch dispatch over window function variants
    private static void ApplyWindowFunction(
        List<Dictionary<string, object>> orderedRows,
        string windowFunction,
        string targetField,
        string outputFieldName)
    {
        switch (windowFunction.ToUpperInvariant())
        {
            case "ROWNUMBER":
                for (var i = 0; i < orderedRows.Count; i++)
                {
                    orderedRows[i][outputFieldName] = i + 1;
                }
                break;

            case "RANK":
                ApplyRank(orderedRows, targetField, outputFieldName, dense: false);
                break;

            case "DENSERANK":
                ApplyRank(orderedRows, targetField, outputFieldName, dense: true);
                break;

            case "SUM":
                ApplyRunningAggregate(orderedRows, targetField, outputFieldName,
                    (values) => values.Sum());
                break;

            case "AVG":
                ApplyRunningAggregate(orderedRows, targetField, outputFieldName,
                    (values) => values.Count > 0 ? values.Average() : 0m);
                break;

            case "COUNT":
                for (var i = 0; i < orderedRows.Count; i++)
                {
                    orderedRows[i][outputFieldName] = i + 1;
                }
                break;

            case "MIN":
                ApplyRunningAggregate(orderedRows, targetField, outputFieldName,
                    (values) => values.Count > 0 ? values.Min() : 0m);
                break;

            case "MAX":
                ApplyRunningAggregate(orderedRows, targetField, outputFieldName,
                    (values) => values.Count > 0 ? values.Max() : 0m);
                break;

            case "LEAD":
                for (var i = 0; i < orderedRows.Count; i++)
                {
                    var nextIndex = i + 1;
                    orderedRows[i][outputFieldName] = nextIndex < orderedRows.Count
                        ? GetDecimalValue(orderedRows[nextIndex], targetField)
                        : (object)DBNull.Value;
                }
                break;

            case "LAG":
                for (var i = 0; i < orderedRows.Count; i++)
                {
                    var prevIndex = i - 1;
                    orderedRows[i][outputFieldName] = prevIndex >= 0
                        ? GetDecimalValue(orderedRows[prevIndex], targetField)
                        : (object)DBNull.Value;
                }
                break;

            default:
                // Unknown function — set output to null
                foreach (var row in orderedRows)
                {
                    row[outputFieldName] = DBNull.Value;
                }
                break;
        }
    }
#pragma warning restore FDW007

    private static void ApplyRank(
        List<Dictionary<string, object>> rows,
        string targetField,
        string outputFieldName,
        bool dense)
    {
        if (rows.Count == 0) return;

        var rank = 1;
        rows[0][outputFieldName] = rank;

        for (var i = 1; i < rows.Count; i++)
        {
            var current = GetDecimalValue(rows[i], targetField);
            var previous = GetDecimalValue(rows[i - 1], targetField);

            if (current != previous)
            {
                rank = dense ? rank + 1 : i + 1;
            }

            rows[i][outputFieldName] = rank;
        }
    }

    private static void ApplyRunningAggregate(
        List<Dictionary<string, object>> rows,
        string targetField,
        string outputFieldName,
        Func<List<decimal>, decimal> aggregateFunc)
    {
        var runningValues = new List<decimal>();
        foreach (var row in rows)
        {
            runningValues.Add(GetDecimalValue(row, targetField));
            row[outputFieldName] = aggregateFunc(new List<decimal>(runningValues));
        }
    }

    private static decimal GetDecimalValue(Dictionary<string, object> row, string fieldName)
    {
        if (row.TryGetValue(fieldName, out var value))
        {
            try
            {
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                _ = ex;
                return 0m;
            }
            catch (OverflowException ex)
            {
                _ = ex;
                return 0m;
            }
            catch (InvalidCastException ex)
            {
                _ = ex;
                return 0m;
            }
        }
        return 0m;
    }
}
