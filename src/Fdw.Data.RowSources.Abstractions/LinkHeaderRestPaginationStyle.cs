using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>Link header based (RFC 5988).</summary>
[TypeOption(typeof(RestPaginationStyles), "LinkHeader")]
[ExcludeFromCodeCoverage]
public sealed class LinkHeaderRestPaginationStyle : RestPaginationStyleBase
{
    /// <summary>Initializes a new instance of <see cref="LinkHeaderRestPaginationStyle"/>.</summary>
    public LinkHeaderRestPaginationStyle() : base(4, "LinkHeader") { }
}
