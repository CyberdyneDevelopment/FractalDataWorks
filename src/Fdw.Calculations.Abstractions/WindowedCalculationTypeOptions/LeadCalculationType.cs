using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>The column's value in the next row of the ordered rows; the last row has none.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "Lead", RestrictToCurrentCompilation = true)]
public sealed class LeadCalculationType : WindowedCalculationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="LeadCalculationType"/> class.</summary>
    public LeadCalculationType() : base(
        name: "Lead",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var values = WindowedColumn.Read(rows, col);
            var result = new List<decimal?>(values.Count);
            for (var i = 0; i < values.Count; i++)
                result.Add(i + 1 < values.Count ? values[i + 1] : null);
            return result;
        },
        toSql: (col, _) => $"LEAD({col}) OVER (ORDER BY {col})")
    { }
}
