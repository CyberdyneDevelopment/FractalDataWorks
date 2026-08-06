using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>
/// Row number calculation type - assigns a sequential integer to each row starting at 1.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WindowedCalculationTypes), "RowNumber", RestrictToCurrentCompilation = true)]
public sealed class RowNumberCalculationType : WindowedCalculationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RowNumberCalculationType"/> class.
    /// </summary>
    public RowNumberCalculationType() : base(
        id: 2,
        name: "RowNumber",
        category: "Windowed",
        calculate: (rows, col, _) =>
        {
            var result = new List<decimal>(rows.Count);
            for (var i = 0; i < rows.Count; i++)
                result.Add(i + 1);
            return result;
        },
        toSql: (col, _) => $"ROW_NUMBER() OVER (ORDER BY {col})")
    { }
}
