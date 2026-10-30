using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>Running count of rows that have a value in the column, over the ordered rows.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "Count", RestrictToCurrentCompilation = true)]
public sealed class CountCalculationType : WindowedCalculationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CountCalculationType"/> class.</summary>
    public CountCalculationType() : base(
        name: "Count",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var result = new List<decimal?>(rows.Count);
            var count = 0;
            foreach (var v in WindowedColumn.Read(rows, col))
            {
                if (v.HasValue)
                    count++;
                result.Add(count);
            }
            return result;
        },
        toSql: (col, _) => $"COUNT({col}) OVER (ORDER BY {col})")
    { }
}
