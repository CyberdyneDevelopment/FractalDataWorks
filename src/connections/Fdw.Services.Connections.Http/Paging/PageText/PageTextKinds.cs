using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Paging.PageText;

/// <summary>
/// The kinds of JSON value a page's body may hold where it names a text, found by the value's own kind (<c>ByValueKind</c>); a kind that
/// matches none (an object, an array, a boolean) gets <c>NotFound</c> and the body is refused as unreadable.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PageTextKindBase), typeof(IPageTextKind), typeof(PageTextKinds))]
public abstract partial class PageTextKinds : TypeCollectionBase<PageTextKindBase, IPageTextKind>
{
}
