using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>Page number style: ?page=3&amp;per_page=50</summary>
[TypeOption(typeof(RestPaginationStyles), "PageNumber")]
[ExcludeFromCodeCoverage]
public sealed class PageNumberRestPaginationStyle : RestPaginationStyleBase
{
    /// <summary>Initializes a new instance of <see cref="PageNumberRestPaginationStyle"/>.</summary>
    public PageNumberRestPaginationStyle() : base(2, "PageNumber") { }
}
