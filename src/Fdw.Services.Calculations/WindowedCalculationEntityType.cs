using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations.Abstractions;
using Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;
using Fdw.Collections.Attributes;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Messages;
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
    public override IReadOnlyList<CalculationSetting> Settings { get; } =
    [
        new CalculationSetting(
            "targetField", "Field", true,
            "The field of the calculation's DataSet input that the window function reads."),
        new CalculationSetting(
            "windowFunction", "WindowedCalculationTypes", true,
            "The window function to apply: a choice from WindowedCalculationTypes.")
    ];

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

        if (WindowedCalculationTypes.ByName(configuration.WindowFunction).Id == 0)
        {
            return GenericResult.Failure(
                CalculationEntityLog.WindowFunctionNotOffered(
                    _logger,
                    "Windowed",
                    configuration.WindowFunction,
                    WindowedCalculationTypes.OfferedNames));
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

        var supplied = SpecFor(entity, context);
        if (!supplied.IsSuccess || supplied.Value is not { } spec) return supplied.ToNewResult<string>();

        var function = WindowedCalculationTypes.ByName(spec.WindowFunction);
        if (function.Id == 0)
        {
            return GenericResult<string>.Failure(
                CalculationEntityLog.WindowFunctionNotOffered(
                    _logger, entity.Name, spec.WindowFunction, WindowedCalculationTypes.OfferedNames));
        }

        try
        {
            var rows = CollectRows(inputs);
            if (rows.Count == 0)
            {
                return GenericResult<string>.Failure(CalculationEntityLog.NoInputRows(_logger, entity.Name, "Windowed"));
            }

            var refusal = CheckColumns(entity, spec, rows);
            if (refusal is not null)
            {
                return GenericResult<string>.Failure(refusal);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return GenericResult<string>.Success(Apply(entity, spec, function, rows));
        }
        catch (Exception ex)
        {
            return GenericResult<string>.Failure(
                CalculationEntityLog.WindowedExecutionFailed(_logger, ex, entity.Name));
        }
    }

    // What the run is told to compute: the spec the caller put in the execution context, else the calculation's stored configuration.
    private IGenericResult<WindowedCalculationSpec> SpecFor(ICalculationEntity entity, ICalculationContext context)
    {
        if (context.Parameters is { } parameters && parameters.TryGetValue(WindowedCalculationSpec.ParameterName, out var supplied))
        {
            return supplied is WindowedCalculationSpec given
                ? GenericResult<WindowedCalculationSpec>.Success(given)
                : GenericResult<WindowedCalculationSpec>.Failure(
                    CalculationEntityLog.WindowedSpecInvalid(_logger, entity.Name, supplied is null ? "null" : supplied.GetType().Name));
        }

        if (entity.TypedConfiguration is not WindowedCalculationConfiguration configuration)
        {
            return GenericResult<WindowedCalculationSpec>.Failure(
                CalculationEntityLog.WindowedConfigurationNotLoaded(_logger, entity.Name));
        }

        return GenericResult<WindowedCalculationSpec>.Success(new WindowedCalculationSpec
        {
            TargetField = configuration.TargetField,
            WindowFunction = configuration.WindowFunction,
            OutputFieldName = entity.Output.ResultFieldName
        });
    }

    private static List<Dictionary<string, object?>> CollectRows(IReadOnlyList<ResolvedCalculationInput> inputs)
    {
        var rows = new List<Dictionary<string, object?>>();
        foreach (var input in inputs)
        {
            if (input.ResolvedValue is IEnumerable<Dictionary<string, object>> dataRows)
            {
                rows.AddRange(dataRows.Select(row => row.ToDictionary(pair => pair.Key, pair => (object?)pair.Value, StringComparer.Ordinal)));
            }
        }

        return rows;
    }

    private IGenericMessage? CheckColumns(
        ICalculationEntity entity,
        WindowedCalculationSpec spec,
        List<Dictionary<string, object?>> rows)
    {
        if (string.IsNullOrWhiteSpace(spec.OutputFieldName))
        {
            return CalculationEntityLog.CalculationValidationFailed(
                _logger, entity.Name, "The windowed calculation names no output field");
        }

        var named = new List<string> { spec.TargetField };
        named.AddRange(spec.PartitionByFields);
        named.AddRange(spec.OrderByFields.Select(order => order.FieldName));
        var absent = named.Find(field => !rows.Exists(row => row.ContainsKey(field)));
        return absent is null
            ? null
            : CalculationEntityLog.CalculationValidationFailed(_logger, entity.Name, $"'{absent}' is not a column of the input rows");
    }

    private string Apply(
        ICalculationEntity entity,
        WindowedCalculationSpec spec,
        IWindowedCalculationType function,
        List<Dictionary<string, object?>> rows)
    {
        var partitions = Partition(rows, spec.PartitionByFields);
        CalculationEntityLog.WindowedExecutionStarted(_logger, entity.Name, function.Name, partitions.Count);

        foreach (var partition in partitions)
        {
            var ordered = Order(partition, spec.OrderByFields);
            var values = function.Calculate(
                ordered.ConvertAll(row => (IDataRow)DataRow.SingleField(spec.TargetField, row.TryGetValue(spec.TargetField, out var raw) ? raw : null)),
                spec.TargetField);

            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i][spec.OutputFieldName] = values[i];
            }
        }

        CalculationEntityLog.WindowedExecutionSucceeded(_logger, entity.Name, rows.Count);
        return JsonSerializer.Serialize(new
        {
            CalculationName = entity.Name,
            WindowFunction = function.Name,
            ResultField = spec.OutputFieldName,
            PartitionCount = partitions.Count,
            RowCount = rows.Count,
            Rows = rows
        });
    }

    // The rows grouped by the values of the partition fields, in the order each group first appears; no fields is one group.
    private static List<List<Dictionary<string, object?>>> Partition(
        List<Dictionary<string, object?>> rows,
        IReadOnlyList<string> partitionByFields)
    {
        if (partitionByFields.Count == 0)
        {
            return [rows];
        }

        return [.. rows
            .GroupBy(row => string.Join("\u001f", partitionByFields.Select(field => row.GetValueOrDefault(field)?.ToString())), StringComparer.Ordinal)
            .Select(group => group.ToList())];
    }

    private static List<Dictionary<string, object?>> Order(
        List<Dictionary<string, object?>> partition,
        IReadOnlyList<WindowOrderField> orderByFields)
    {
        IOrderedEnumerable<Dictionary<string, object?>>? ordered = null;
        foreach (var field in orderByFields)
        {
            ordered = ordered is null ? OrderFirst(partition, field) : OrderThen(ordered, field);
        }

        return ordered is null ? partition : ordered.ToList();
    }

    private static IOrderedEnumerable<Dictionary<string, object?>> OrderFirst(
        List<Dictionary<string, object?>> partition, WindowOrderField field)
        => field.Descending
            ? partition.OrderByDescending(row => row.GetValueOrDefault(field.FieldName), ByValue)
            : partition.OrderBy(row => row.GetValueOrDefault(field.FieldName), ByValue);

    private static IOrderedEnumerable<Dictionary<string, object?>> OrderThen(
        IOrderedEnumerable<Dictionary<string, object?>> ordered, WindowOrderField field)
        => field.Descending
            ? ordered.ThenByDescending(row => row.GetValueOrDefault(field.FieldName), ByValue)
            : ordered.ThenBy(row => row.GetValueOrDefault(field.FieldName), ByValue);

    private static readonly Comparer<object?> ByValue = Comparer<object?>.Create(CompareValues);

    // An empty value sorts first; otherwise the values compare as themselves, and values that cannot compare fail the run.
    private static int CompareValues(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return left is null ? (right is null ? 0 : -1) : 1;
        }

        return left is IComparable comparable
            ? comparable.CompareTo(right)
            : throw new InvalidOperationException($"Values of type {left.GetType().Name} cannot be ordered");
    }
}
