using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>Writes a sample of unmatched keys for a message (FDW-4988): each key as its columns and values, the keys separated by semicolons.</summary>
internal static class UnmatchedKeyText
{
    /// <summary>Writes the keys; an empty sample (a sample size of 0) writes that none is shown.</summary>
    /// <param name="keys">The keys.</param>
    /// <returns>The text.</returns>
    internal static string Of(IReadOnlyList<IReadOnlyDictionary<string, object?>> keys) =>
        keys.Count == 0
            ? "no key is shown (the sample size is 0)"
            : string.Join("; ", keys.Select(key => string.Join(", ", key.Select(column => string.Concat(column.Key, "=", Convert.ToString(column.Value, CultureInfo.InvariantCulture))))));
}
