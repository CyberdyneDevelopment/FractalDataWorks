using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>One output row per distinct group of a table's rows, with the group's key columns readable and totals taken over the group.</summary>
public sealed class EachGroupSource : RowSourceDefinition
{
    private readonly string _table;
    private readonly IReadOnlyList<string> _by;

    /// <summary>Initializes a new instance of the <see cref="EachGroupSource"/> class.</summary>
    /// <param name="table">The table that is grouped.</param>
    /// <param name="by">The columns whose values define a group.</param>
    public EachGroupSource(string table, IReadOnlyList<string> by)
    {
        _table = table;
        _by = by;
    }

    /// <inheritdoc />
    public override string Kind => "EachGroup";

    /// <inheritdoc />
    public override IReadOnlyList<string> GroupBy => _by;

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

        var groups = new Dictionary<string, List<IReadOnlyDictionary<string, object?>>>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var row in rows.Value!)
        {
            var key = KeyOf(output, row);
            if (key.IsFailure)
            {
                return key.ToNewResult<IReadOnlyList<DriverRow>>();
            }

            if (!groups.TryGetValue(key.Value!, out var members))
            {
                members = [];
                groups[key.Value!] = members;
                order.Add(key.Value!);
            }

            members.Add(row);
        }

        return Build(output, scope, groups, order);
    }

    private IGenericResult<string> KeyOf(string output, IReadOnlyDictionary<string, object?> row)
    {
        var parts = new List<string>(_by.Count);
        foreach (var column in _by)
        {
            if (!row.TryGetValue(column, out var value))
            {
                return SetFailure.Of<string>("SetColumnMissing", output, $"table '{_table}' has a row without group column '{column}'");
            }

            parts.Add(SetRunScope.KeyText(value));
        }

        return GenericResult<string>.Success(string.Join("\u001f", parts));
    }

    private IGenericResult<IReadOnlyList<DriverRow>> Build(
        string output, SetRunScope scope, Dictionary<string, List<IReadOnlyDictionary<string, object?>>> groups, List<string> order)
    {
        var limit = WithinLimit(output, order.Count, scope);
        if (limit.IsFailure)
        {
            return limit.ToNewResult<IReadOnlyList<DriverRow>>();
        }

        var drivers = new List<DriverRow>(order.Count);
        foreach (var key in order)
        {
            var members = groups[key];
            var record = SetRecord.Merge(scope.Scalars, new Dictionary<string, object?>(StringComparer.Ordinal));
            foreach (var column in _by)
            {
                record[column] = members[0][column];
            }

            drivers.Add(new DriverRow(record, _table, members));
        }

        return GenericResult<IReadOnlyList<DriverRow>>.Success(drivers);
    }
}
