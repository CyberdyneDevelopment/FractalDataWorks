using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>The name contains the search text.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ListMatches), "Contains")]
public sealed class ContainsListMatch : ListMatchBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainsListMatch"/> class.</summary>
    public ContainsListMatch()
        : base("Contains", "contains", "Contains")
    {
    }
}
