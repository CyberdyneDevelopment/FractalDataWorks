using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>Running minimum of the column over the ordered rows; the minimum is empty until a row has a value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "Min", RestrictToCurrentCompilation = true)]
public sealed class MinCalculationType : WindowedCalculationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MinCalculationType"/> class.</summary>
    public MinCalculationType() : base(
        name: "Min",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var result = new List<decimal?>(rows.Count);
            decimal? min = null;
            foreach (var v in WindowedColumn.Read(rows, col))
            {
                if (v.HasValue)
                    min = min.HasValue && min.Value < v.Value ? min.Value : v.Value;
                result.Add(min);
            }
            return result;
        },
        toSql: (col, _) => $"MIN({col}) OVER (ORDER BY {col})")
    { }
}
