using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>Takes the mean of the values; there is none for an empty column.</summary>
public sealed class AverageValue : AggregateValueDefinition
{
    /// <summary>Initializes a new instance of the <see cref="AverageValue"/> class.</summary>
    /// <param name="name">The name formulas read the value under.</param>
    /// <param name="table">The table the column is in.</param>
    /// <param name="column">The column that is totalled.</param>
    public AverageValue(string name, string table, string column)
        : base(name, table, column)
    {
    }

    /// <inheritdoc />
    public override string Kind => "Average";

    /// <inheritdoc />
    protected override decimal? Fold(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        decimal total = 0m;
        foreach (var value in values)
        {
            total += value;
        }

        return total / values.Count;
    }
}
