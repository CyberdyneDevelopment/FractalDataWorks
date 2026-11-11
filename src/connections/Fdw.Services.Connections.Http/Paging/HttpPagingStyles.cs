using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>
/// The ways a remote HTTP API pages its rows (FDW-4938): SingleResponse, OffsetLimit, PageNumber, LinkHeader, BodyNextLink and
/// ContinuationToken. A connection names one in <c>PagingStyle</c>; there is no default style, and a paged or streamed read of a connection
/// that names none is refused.
/// </summary>
/// <remarks>
/// Each option declares the additional properties it reads (<see cref="HttpPagingStyleBase.ExpectedProperties"/>), refuses a connection that
/// leaves out one it needs, and builds each read's plan. The source generator emits <c>ByName</c>, <c>All</c> and <c>NotFound</c>.
/// Why here and not beside <c>HttpProtocols</c> in Http.Abstractions: the styles declare their tuning defaults (<c>IDeclaresDefaults</c>),
/// which that netstandard2.0 assembly cannot reach; the authentication methods, which read the same dictionary, live in this assembly too.
/// </remarks>
[TypeCollection(typeof(HttpPagingStyleBase), typeof(HttpPagingStyleBase), typeof(HttpPagingStyles))]
public abstract partial class HttpPagingStyles : TypeCollectionBase<HttpPagingStyleBase, HttpPagingStyleBase>
{
}
