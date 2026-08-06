using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>Offset/limit style: ?offset=100&amp;limit=50</summary>
[TypeOption(typeof(RestPaginationStyles), "OffsetLimit")]
[ExcludeFromCodeCoverage]
public sealed class OffsetLimitRestPaginationStyle : RestPaginationStyleBase
{
    /// <summary>Initializes a new instance of <see cref="OffsetLimitRestPaginationStyle"/>.</summary>
    public OffsetLimitRestPaginationStyle() : base(1, "OffsetLimit") { }
}
