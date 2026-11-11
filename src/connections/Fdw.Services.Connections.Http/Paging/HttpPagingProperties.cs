namespace Fdw.Services.Connections.Http.Paging;

/// <summary>
/// The names of the additional properties (<c>conn.HttpConnectionProperty</c>) the HTTP paging styles read (FDW-4938).
/// </summary>
/// <remarks>
/// Why a "Paging" prefix: the properties share one dictionary with the authentication method's keys (SecretManagerName, ApiKeySecretName...),
/// so no paging name can collide with one. Which keys a connection's style reads is what the style declares
/// (<see cref="HttpPagingStyleBase.ExpectedProperties"/>), never read off the prefix.
/// </remarks>
public static class HttpPagingProperties
{
    /// <summary>The rows asked for in a page. A tuning value: a style that asks for a size declares its default.</summary>
    public const string PageSize = "PagingPageSize";

    /// <summary>The most pages one read may take before it is stopped as not finishing. A tuning value each paging style declares.</summary>
    public const string MaxPages = "PagingMaxPages";

    /// <summary>The query parameter that carries the rows to skip (offset, $skip, start...).</summary>
    public const string OffsetParameter = "PagingOffsetParameter";

    /// <summary>The query parameter that carries the rows to return (limit, $top, count...).</summary>
    public const string LimitParameter = "PagingLimitParameter";

    /// <summary>The query parameter that carries the page number (page, page[number]...).</summary>
    public const string PageNumberParameter = "PagingPageNumberParameter";

    /// <summary>The query parameter that carries the page size (per_page, page[size], pageSize...).</summary>
    public const string PageSizeParameter = "PagingPageSizeParameter";

    /// <summary>The number of the remote's first page, 0 or 1 as the remote counts.</summary>
    public const string FirstPageNumber = "PagingFirstPageNumber";

    /// <summary>The path in a page's JSON body of the next page's address (@odata.nextLink, links.next, next...).</summary>
    public const string NextLinkPath = "PagingNextLinkPath";

    /// <summary>The path in a page's JSON body of the continuation token (nextPageToken, meta.next_cursor...).</summary>
    public const string TokenPath = "PagingTokenPath";

    /// <summary>The query parameter the continuation token is sent back in (pageToken, cursor, after...).</summary>
    public const string TokenParameter = "PagingTokenParameter";
}
