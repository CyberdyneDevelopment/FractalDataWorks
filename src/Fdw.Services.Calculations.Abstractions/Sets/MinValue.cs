using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>Takes the smallest value; there is none for an empty column.</summary>
public sealed class MinValue : AggregateValueDefinition
{
    /// <summary>Initializes a new instance of the <see cref="MinValue"/> class.</summary>
    /// <param name="name">The name formulas read the value under.</param>
    /// <param name="table">The table the column is in.</param>
    /// <param name="column">The column that is totalled.</param>
    public MinValue(string name, string table, string column)
        : base(name, table, column)
    {
    }

    /// <inheritdoc />
    public override string Kind => "Min";

    /// <inheritdoc />
    protected override decimal? Fold(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var smallest = values[0];
        foreach (var value in values)
        {
            smallest = value < smallest ? value : smallest;
        }

        return smallest;
    }
}
