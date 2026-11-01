using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>The name is the search text, whole.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ListMatches), "Exact")]
public sealed class ExactListMatch : ListMatchBase
{
    /// <summary>Initializes a new instance of the <see cref="ExactListMatch"/> class.</summary>
    public ExactListMatch()
        : base("Exact", "exact", "Equal")
    {
    }
}
