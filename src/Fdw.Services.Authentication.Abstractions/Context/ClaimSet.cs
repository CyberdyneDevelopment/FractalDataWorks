using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// The claims gathered about a principal, each retaining its provenance.
/// </summary>
/// <remarks>
/// Merging never collapses sources. Two providers asserting the same claim type stay distinguishable,
/// so an authorization step can read only what it is entitled to trust rather than whatever arrived
/// last.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record ClaimSet
{
    /// <summary>An empty set.</summary>
    public static ClaimSet Empty { get; } = new();

    /// <summary>Gets every claim, in the order contributed.</summary>
    public ImmutableArray<Claim> Claims { get; init; } = [];

    /// <summary>Returns a set with <paramref name="claims"/> added.</summary>
    /// <param name="claims">The claims to add.</param>
    public ClaimSet Add(IEnumerable<Claim> claims)
        => this with { Claims = [.. Claims, .. claims] };

    /// <summary>Returns the values of <paramref name="type"/> from the given sources only.</summary>
    /// <param name="type">The claim type.</param>
    /// <param name="sources">The sources to trust for this read.</param>
    /// <remarks>
    /// There is deliberately no overload that reads a type without naming its sources: a caller that
    /// does not say what it trusts has not decided, and the default would decide for it.
    /// </remarks>
    public IReadOnlyList<string> Values(string type, params ClaimSource[] sources)
        => [.. Claims.Where(c => c.Type == type && sources.Contains(c.Source)).Select(c => c.Value)];
}
