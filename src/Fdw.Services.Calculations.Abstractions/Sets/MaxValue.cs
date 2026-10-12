using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>Takes the largest value; there is none for an empty column.</summary>
public sealed class MaxValue : AggregateValueDefinition
{
    /// <summary>Initializes a new instance of the <see cref="MaxValue"/> class.</summary>
    /// <param name="name">The name formulas read the value under.</param>
    /// <param name="table">The table the column is in.</param>
    /// <param name="column">The column that is totalled.</param>
    public MaxValue(string name, string table, string column)
        : base(name, table, column)
    {
    }

    /// <inheritdoc />
    public override string Kind => "Max";

    /// <inheritdoc />
    protected override decimal? Fold(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var largest = values[0];
        foreach (var value in values)
        {
            largest = value > largest ? value : largest;
        }

        return largest;
    }
}
