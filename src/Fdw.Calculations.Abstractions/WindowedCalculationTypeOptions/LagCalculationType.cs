using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>The column's value in the previous row of the ordered rows; the first row has none.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "Lag", RestrictToCurrentCompilation = true)]
public sealed class LagCalculationType : WindowedCalculationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="LagCalculationType"/> class.</summary>
    public LagCalculationType() : base(
        name: "Lag",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var values = WindowedColumn.Read(rows, col);
            var result = new List<decimal?>(values.Count);
            for (var i = 0; i < values.Count; i++)
                result.Add(i > 0 ? values[i - 1] : null);
            return result;
        },
        toSql: (col, _) => $"LAG({col}) OVER (ORDER BY {col})")
    { }
}
