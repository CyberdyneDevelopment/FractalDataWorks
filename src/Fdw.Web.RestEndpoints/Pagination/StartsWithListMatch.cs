using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>The name starts with the search text.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ListMatches), "StartsWith")]
public sealed class StartsWithListMatch : ListMatchBase
{
    /// <summary>Initializes a new instance of the <see cref="StartsWithListMatch"/> class.</summary>
    public StartsWithListMatch()
        : base("StartsWith", "startsWith", "StartsWith")
    {
    }
}
