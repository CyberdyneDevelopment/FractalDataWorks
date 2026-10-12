using System;
using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>Counts the values that are not empty.</summary>
public sealed class CountValue : AggregateValueDefinition
{
    /// <summary>Initializes a new instance of the <see cref="CountValue"/> class.</summary>
    /// <param name="name">The name formulas read the value under.</param>
    /// <param name="table">The table the column is in.</param>
    /// <param name="column">The column that is totalled.</param>
    public CountValue(string name, string table, string column)
        : base(name, table, column)
    {
    }

    /// <inheritdoc />
    public override string Kind => "Count";

    /// <summary>A count asks only that a cell holds a value, so any type counts; an empty cell is not counted.</summary>
    /// <param name="output">The output being built.</param>
    /// <param name="raw">The cell's value.</param>
    /// <returns>One for a cell that holds a value, none for an empty one.</returns>
    protected override IGenericResult<decimal?> Read(string output, object? raw) =>
        GenericResult<decimal?>.Success(raw is null || raw is DBNull ? null : 1m);

    /// <inheritdoc />
    protected override decimal? Fold(IReadOnlyList<decimal> values)
    {
        return values.Count;
    }
}
