using System;
using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A total over one column of a table. Over a group's rows when the output is one row per group of that table, otherwise over the whole table.</summary>
public abstract class AggregateValueDefinition : DerivedValueDefinition
{
    /// <summary>Initializes a new instance of the <see cref="AggregateValueDefinition"/> class.</summary>
    /// <param name="name">The name formulas read the value under.</param>
    /// <param name="table">The table the column is in.</param>
    /// <param name="column">The column that is totalled.</param>
    protected AggregateValueDefinition(string name, string table, string column)
        : base(name, table)
    {
        Column = column;
    }

    /// <inheritdoc />
    public sealed override string Column { get; }

    /// <inheritdoc />
    public sealed override IGenericResult<object?> Compute(string output, DriverRow driver, SetRunScope scope)
    {
        if (driver.ScopeRows is not null && string.Equals(driver.ScopeTable, Table, StringComparison.Ordinal))
        {
            return Total(output, driver.ScopeRows);
        }

        var memoKey = string.Concat(GetType().Name, "\u001f", Table, "\u001f", Column);
        if (scope.Memo.TryGetValue(memoKey, out var known))
        {
            return GenericResult<object?>.Success(known);
        }

        var table = scope.Table(output, Table);
        if (table.IsFailure)
        {
            return table.ToNewResult<object?>();
        }

        var total = Total(output, table.Value!);
        if (total.IsSuccess)
        {
            scope.Memo[memoKey] = total.Value;
        }

        return total;
    }

    private IGenericResult<object?> Total(string output, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var values = Collect(output, rows);
        return values.IsFailure ? values.ToNewResult<object?>() : GenericResult<object?>.Success(Fold(values.Value!));
    }

    /// <summary>Folds the non-empty values of the column into the total.</summary>
    /// <param name="values">The values, in row order.</param>
    /// <returns>The total, or null when there is nothing to total and no total of nothing exists.</returns>
    protected abstract decimal? Fold(IReadOnlyList<decimal> values);

    /// <summary>
    /// Reads one cell of the column for this aggregate. A total, average, minimum or maximum needs an exact number; a kind whose
    /// answer does not depend on the value (a count) overrides this to accept any cell that holds one.
    /// </summary>
    /// <param name="output">The output being built, which places a refusal.</param>
    /// <param name="raw">The cell's value.</param>
    /// <returns>The number it stands for, or none for an empty cell.</returns>
    protected virtual IGenericResult<decimal?> Read(string output, object? raw) => SetNumbers.Read(output, Column, raw);

    private IGenericResult<List<decimal>> Collect(string output, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var values = new List<decimal>(rows.Count);
        foreach (var row in rows)
        {
            if (!row.TryGetValue(Column, out var raw))
            {
                return SetFailure.Of<List<decimal>>("SetColumnMissing", output, $"table '{Table}' has a row without column '{Column}'");
            }

            var number = Read(output, raw);
            if (number.IsFailure)
            {
                return number.ToNewResult<List<decimal>>();
            }

            if (number.Value is { } value)
            {
                values.Add(value);
            }
        }

        return GenericResult<List<decimal>>.Success(values);
    }
}
