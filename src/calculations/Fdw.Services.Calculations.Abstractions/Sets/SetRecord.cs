using System;
using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>Builds the one record a row's formulas read: the single values first, then the row's own values over them.</summary>
public static class SetRecord
{
    /// <summary>Merges the single values and a row.</summary>
    /// <param name="scalars">The single values.</param>
    /// <param name="row">The row's values; they win over a single value of the same name.</param>
    /// <returns>A new record.</returns>
    public static Dictionary<string, object?> Merge(IReadOnlyDictionary<string, object?> scalars, IReadOnlyDictionary<string, object?> row)
    {
        var record = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var pair in scalars)
        {
            record[pair.Key] = pair.Value;
        }

        foreach (var pair in row)
        {
            record[pair.Key] = pair.Value;
        }

        return record;
    }
}
