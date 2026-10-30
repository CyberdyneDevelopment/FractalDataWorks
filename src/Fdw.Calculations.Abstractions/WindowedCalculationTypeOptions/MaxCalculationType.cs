using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>Running maximum of the column over the ordered rows; the maximum is empty until a row has a value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "Max", RestrictToCurrentCompilation = true)]
public sealed class MaxCalculationType : WindowedCalculationTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MaxCalculationType"/> class.</summary>
    public MaxCalculationType() : base(
        name: "Max",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var result = new List<decimal?>(rows.Count);
            decimal? max = null;
            foreach (var v in WindowedColumn.Read(rows, col))
            {
                if (v.HasValue)
                    max = max.HasValue && max.Value > v.Value ? max.Value : v.Value;
                result.Add(max);
            }
            return result;
        },
        toSql: (col, _) => $"MAX({col}) OVER (ORDER BY {col})")
    { }
}
