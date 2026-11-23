using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Writes the names of a kind collection the way a refusal reads them: "Sum, Count or Lookup".</summary>
internal static class KindNames
{
    /// <summary>Joins names with commas and a closing "or".</summary>
    /// <param name="names">The names, in the collection's order.</param>
    /// <returns>The joined names.</returns>
    internal static string Of(IEnumerable<string> names)
    {
        var all = names.ToList();
        return all.Count < 2 ? string.Concat(all) : $"{string.Join(", ", all.Take(all.Count - 1))} or {all[all.Count - 1]}";
    }
}
