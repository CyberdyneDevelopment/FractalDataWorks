using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>Adds up the values; the total of nothing is 0.</summary>
public sealed class SumValue : AggregateValueDefinition
{
    /// <summary>Initializes a new instance of the <see cref="SumValue"/> class.</summary>
    /// <param name="name">The name formulas read the value under.</param>
    /// <param name="table">The table the column is in.</param>
    /// <param name="column">The column that is totalled.</param>
    public SumValue(string name, string table, string column)
        : base(name, table, column)
    {
    }

    /// <inheritdoc />
    public override string Kind => "Sum";

    /// <inheritdoc />
    protected override decimal? Fold(IReadOnlyList<decimal> values)
    {
        decimal total = 0m;
        foreach (var value in values)
        {
            total += value;
        }

        return total;
    }
}
