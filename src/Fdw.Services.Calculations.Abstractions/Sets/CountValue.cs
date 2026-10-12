using System.Collections.Generic;

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

    /// <inheritdoc />
    protected override decimal? Fold(IReadOnlyList<decimal> values)
    {
        return values.Count;
    }
}
