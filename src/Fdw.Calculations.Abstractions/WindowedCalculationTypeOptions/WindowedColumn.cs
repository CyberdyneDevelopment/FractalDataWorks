using System.Collections.Generic;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Calculations.Abstractions.WindowedCalculationTypeOptions;

/// <summary>Reads the column a windowed calculation runs over.</summary>
internal static class WindowedColumn
{
    /// <summary>One entry per row: the column's decimal value, or empty when the row has none.</summary>
    internal static List<decimal?> Read(IReadOnlyList<IDataRow> rows, string columnName)
    {
        var values = new List<decimal?>(rows.Count);
        foreach (var row in rows)
            values.Add(row.TryGetValue<decimal>(columnName, out var value) ? value : null);
        return values;
    }
}
