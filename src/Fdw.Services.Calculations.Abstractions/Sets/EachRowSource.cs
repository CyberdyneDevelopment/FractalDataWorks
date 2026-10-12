using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>One output row per input row of a table, optionally dropping the rows a condition rejects.</summary>
public sealed class EachRowSource : RowSourceDefinition
{
    private readonly string _table;
    private readonly CalculationNode? _condition;
    private readonly CalculationNode[] _where;

    /// <summary>Initializes a new instance of the <see cref="EachRowSource"/> class.</summary>
    /// <param name="table">The table whose rows drive the output.</param>
    /// <param name="where">A condition a row must meet to produce an output row, or null to keep every row.</param>
    public EachRowSource(string table, CalculationNode? where = null)
    {
        _table = table;
        _condition = where;
        _where = where is null
            ? []
            : [CalculationNode.Of("When", where, CalculationNode.FromDecimal(1m), CalculationNode.FromDecimal(0m))];
    }

    /// <inheritdoc />
    public override string Kind => "Each";

    /// <inheritdoc />
    public override CalculationNode? Condition => _condition;

    /// <inheritdoc />
    public override IReadOnlyList<CalculationNode> Formulas => _where;

    /// <inheritdoc />
    public override string? TableName => _table;

    /// <inheritdoc />
    public override IGenericResult<IReadOnlyList<DriverRow>> Drivers(string output, SetRunScope scope, IReadOnlyList<ICompiledCalculation> compiled)
    {
        var rows = scope.Table(output, _table);
        if (rows.IsFailure)
        {
            return rows.ToNewResult<IReadOnlyList<DriverRow>>();
        }

        var drivers = new List<DriverRow>(rows.Value!.Count);
        foreach (var row in rows.Value)
        {
            var record = SetRecord.Merge(scope.Scalars, row);
            var kept = Keeps(compiled, record);
            if (kept.IsFailure)
            {
                return kept.ToNewResult<IReadOnlyList<DriverRow>>();
            }

            if (kept.Value is true)
            {
                drivers.Add(new DriverRow(record));
            }
        }

        var limit = WithinLimit(output, drivers.Count, scope);
        return limit.IsFailure ? limit.ToNewResult<IReadOnlyList<DriverRow>>() : GenericResult<IReadOnlyList<DriverRow>>.Success(drivers);
    }

    private static IGenericResult<bool> Keeps(IReadOnlyList<ICompiledCalculation> compiled, IReadOnlyDictionary<string, object?> record)
    {
        if (compiled.Count == 0)
        {
            return GenericResult<bool>.Success(true);
        }

        var value = SetValue.Of(compiled[0], record);
        return value.IsFailure ? value.ToNewResult<bool>() : GenericResult<bool>.Success(value.Value == 1m);
    }
}
