using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>Cursor-based: ?cursor=abc123&amp;limit=50</summary>
[TypeOption(typeof(RestPaginationStyles), "Cursor")]
[ExcludeFromCodeCoverage]
public sealed class CursorRestPaginationStyle : RestPaginationStyleBase
{
    /// <summary>Initializes a new instance of <see cref="CursorRestPaginationStyle"/>.</summary>
    public CursorRestPaginationStyle() : base(3, "Cursor") { }
}
