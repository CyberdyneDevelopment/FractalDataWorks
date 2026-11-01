using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>The kinds one catalog search was asked to find, so a reader runs only for a kind that was asked for (FDW-1681).</summary>
internal sealed class CatalogKindSet
{
    private readonly HashSet<string> _kinds;

    private CatalogKindSet(IReadOnlyList<string> kinds)
    {
        Names = kinds;
        _kinds = new HashSet<string>(kinds, StringComparer.Ordinal);
    }

    /// <summary>Gets the kinds, in the order a browse lists them.</summary>
    public IReadOnlyList<string> Names { get; }

    /// <summary>Reads the kinds a request named.</summary>
    /// <param name="requested">The kinds named, in any case; none means every kind.</param>
    /// <returns>The set, or the first name that is not a kind (null when all are).</returns>
    public static (CatalogKindSet? Kinds, string? Unknown) Of(IEnumerable<string> requested)
    {
        ArgumentNullException.ThrowIfNull(requested);
        var named = requested.Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => name.Trim()).ToList();
        var unknown = named.FirstOrDefault(name => CatalogKinds.IndexOf(name) < 0);
        if (unknown is not null)
        {
            return (null, unknown);
        }

        var chosen = named.Count == 0
            ? CatalogKinds.All
            : CatalogKinds.All.Where(kind => named.Any(name => string.Equals(name, kind, StringComparison.OrdinalIgnoreCase))).ToList();
        return (new CatalogKindSet(chosen), null);
    }

    /// <summary>Whether a kind was asked for.</summary>
    /// <param name="kind">One of <see cref="CatalogKinds"/>.</param>
    /// <returns><see langword="true"/> when the search finds this kind.</returns>
    public bool Wants(string kind) => _kinds.Contains(kind);
}
