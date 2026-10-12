using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>
/// A number of output rows worked out by a formula: N rows, each carrying its position 1 to N in an index column. Either once for
/// the single values, or once for each row of a table.
/// </summary>
public sealed class CountSource : RowSourceDefinition
{
    private readonly CalculationNode[] _count;
    private readonly string _indexColumn;
    private readonly string? _per;

    /// <summary>Initializes a new instance of the <see cref="CountSource"/> class.</summary>
    /// <param name="count">The formula for how many rows; it must give a whole number from 0 up to the row limit.</param>
    /// <param name="indexColumn">The name the position 1 to N is readable under.</param>
    /// <param name="per">A table to repeat the count for each row of, or null to count once for the single values.</param>
    public CountSource(CalculationNode count, string indexColumn, string? per = null)
    {
        _count = [count];
        _indexColumn = indexColumn;
        _per = per;
    }

    /// <inheritdoc />
    public override string Kind => "Count";

    /// <inheritdoc />
    public override string? IndexColumn => _indexColumn;

    /// <inheritdoc />
    public override CalculationNode? CountFormula => _count[0];

    /// <inheritdoc />
    public override IReadOnlyList<CalculationNode> Formulas => _count;

    /// <inheritdoc />
    public override string? TableName => _per;

    /// <inheritdoc />
    public override IGenericResult<IReadOnlyList<DriverRow>> Drivers(string output, SetRunScope scope, IReadOnlyList<ICompiledCalculation> compiled)
    {
        var bases = Bases(output, scope);
        if (bases.IsFailure)
        {
            return bases.ToNewResult<IReadOnlyList<DriverRow>>();
        }

        var drivers = new List<DriverRow>();
        foreach (var record in bases.Value!)
        {
            var counted = Expand(output, scope, compiled[0], record, drivers);
            if (counted.IsFailure)
            {
                return counted.ToNewResult<IReadOnlyList<DriverRow>>();
            }
        }

        return GenericResult<IReadOnlyList<DriverRow>>.Success(drivers);
    }

    private IGenericResult<List<Dictionary<string, object?>>> Bases(string output, SetRunScope scope)
    {
        if (_per is null)
        {
            return GenericResult<List<Dictionary<string, object?>>>.Success([SetRecord.Merge(scope.Scalars, new Dictionary<string, object?>(StringComparer.Ordinal))]);
        }

        var rows = scope.Table(output, _per);
        if (rows.IsFailure)
        {
            return rows.ToNewResult<List<Dictionary<string, object?>>>();
        }

        var records = new List<Dictionary<string, object?>>(rows.Value!.Count);
        foreach (var row in rows.Value)
        {
            records.Add(SetRecord.Merge(scope.Scalars, row));
        }

        return GenericResult<List<Dictionary<string, object?>>>.Success(records);
    }

    private IGenericResult Expand(string output, SetRunScope scope, ICompiledCalculation count, Dictionary<string, object?> record, List<DriverRow> drivers)
    {
        var value = SetValue.Of(count, record);
        if (value.IsFailure)
        {
            return value;
        }

        var n = value.Value;
        if (n < 0m || n != decimal.Truncate(n) || n > scope.Limits.MaxRows)
        {
            return SetFailure.Of("SetRowCountInvalid", output, $"the count formula gave {n}; the limit is {scope.Limits.MaxRows}");
        }

        var limit = WithinLimit(output, drivers.Count + (int)n, scope);
        if (limit.IsFailure)
        {
            return limit;
        }

        for (var position = 1; position <= (int)n; position++)
        {
            var row = new Dictionary<string, object?>(record, StringComparer.Ordinal) { [_indexColumn] = (decimal)position };
            drivers.Add(new DriverRow(row));
        }

        return GenericResult.Success();
    }
}
