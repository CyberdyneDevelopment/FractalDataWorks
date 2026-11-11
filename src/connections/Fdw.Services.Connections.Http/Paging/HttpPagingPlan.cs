using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Connections.Http.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>
/// One read's way through a remote's pages, built by the paging style the connection states (FDW-4938): the address of the first page, then,
/// from each page's outcome, the address of the next, until the remote says it is done.
/// </summary>
/// <remarks>
/// <para>
/// The rules every style shares are kept here so no style can leave one out: a page that holds more rows than the size asked for stops the
/// read (the remote did not honour the size, so the next page cannot be found); an address this read already asked for stops it (it would
/// read the same rows forever); and more pages than <see cref="MaxPages"/> stop it. Each stop is a failure with its own message, never a
/// read returned as complete.
/// </para>
/// <para>A plan is used by one read and holds that read's position; it is not shared.</para>
/// </remarks>
public abstract class HttpPagingPlan
{
    private readonly HashSet<string> _asked = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="HttpPagingPlan"/> class.</summary>
    /// <param name="style">The style that built the plan.</param>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="pageSize">The rows asked for in a page; 0 when the style asks for no size.</param>
    /// <param name="maxPages">The most pages the read may take.</param>
    /// <param name="window">The rows of the set the read returns.</param>
    protected HttpPagingPlan(string style, HttpPagingSettings settings, int pageSize, int maxPages, HttpReadWindow window)
    {
        Style = style;
        ConnectionName = settings.ConnectionName;
        Logger = settings.Logger;
        PageSize = pageSize;
        MaxPages = maxPages;
        Take = window.Take;
    }

    /// <summary>Gets the name of the style that built the plan.</summary>
    public string Style { get; }

    /// <summary>Gets the connection's name, for the messages.</summary>
    public string ConnectionName { get; }

    /// <summary>Gets the rows asked for in a page; 0 when the style asks for no size, and then no page is too large.</summary>
    public int PageSize { get; }

    /// <summary>Gets the most pages the read may take.</summary>
    public int MaxPages { get; }

    /// <summary>Gets the most rows the read returns; null for every row. The read stops asking for pages once it has them.</summary>
    public int? Take { get; }

    /// <summary>
    /// Gets the rows at the start of what the remote returns that the read drops itself, because the style cannot ask the remote to skip them
    /// (a remote that does not page); 0 for a style that asks the remote.
    /// </summary>
    public virtual int SkipHere => 0;

    /// <summary>Gets whether the plan reads a page's body to find the next page, so the reader keeps the page's bytes for it.</summary>
    public virtual bool ReadsBody => false;

    /// <summary>Gets the logger refusals are written to.</summary>
    protected ILogger Logger { get; }

    /// <summary>Gets the address of the first page from the address of the translated query, which carries its filter and ordering.</summary>
    /// <param name="request">The translated query's address, relative to the connection's base address or absolute.</param>
    /// <returns>The first page's address.</returns>
    public abstract Uri FirstPage(Uri request);

    /// <summary>Gets the address of the next page from a page's outcome, or <see langword="null"/> when the remote is done.</summary>
    /// <param name="outcome">What the page returned.</param>
    /// <returns>The next page's address; null when the read is complete; or why the read is stopped.</returns>
    public IGenericResult<Uri?> Next(HttpPageOutcome outcome)
    {
        _asked.Add(outcome.Request.AbsoluteUri);

        if (PageSize > 0 && outcome.Rows > PageSize)
            return GenericResult<Uri?>.Failure(HttpPagingLog.PageLargerThanAsked(Logger, ConnectionName, outcome.Page, PageSize));

        var next = NextPage(outcome);
        if (next.IsFailure || next.Value is null)
            return next;

        if (outcome.Page >= MaxPages)
            return GenericResult<Uri?>.Failure(HttpPagingLog.MaxPagesExceeded(Logger, ConnectionName, Style, MaxPages));

        var absolute = next.Value.IsAbsoluteUri ? next.Value : new Uri(outcome.Request, next.Value);
        return _asked.Contains(absolute.AbsoluteUri)
            ? GenericResult<Uri?>.Failure(HttpPagingLog.NextPageRepeats(Logger, ConnectionName, outcome.Page, absolute.AbsoluteUri))
            : GenericResult<Uri?>.Success(absolute);
    }

    /// <summary>The style's own step: the next page's address from a page's outcome, or null when the remote is done.</summary>
    /// <param name="outcome">What the page returned.</param>
    /// <returns>The next page's address, null when done, or why the read is stopped.</returns>
    protected abstract IGenericResult<Uri?> NextPage(HttpPageOutcome outcome);

    /// <summary>
    /// Follows a link the remote gave: resolved against the page it came from, an http or https address on the same origin, since the
    /// connection's credentials travel with every request it sends.
    /// </summary>
    /// <param name="outcome">The page the link came from.</param>
    /// <param name="link">The link as the remote gave it.</param>
    /// <returns>The next page's absolute address, or why it cannot be followed.</returns>
    protected IGenericResult<Uri?> Follow(HttpPageOutcome outcome, string link)
    {
        if (!Uri.TryCreate(outcome.Request, link, out var next)
            || !(string.Equals(next.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(next.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            return GenericResult<Uri?>.Failure(HttpPagingLog.NextLinkMalformed(Logger, ConnectionName, outcome.Page, link));

        return string.Equals(next.GetLeftPart(UriPartial.Authority), outcome.Request.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)
            ? GenericResult<Uri?>.Success(next)
            : GenericResult<Uri?>.Failure(HttpPagingLog.NextLinkLeavesOrigin(
                Logger, ConnectionName, outcome.Page, next.AbsoluteUri, outcome.Request.GetLeftPart(UriPartial.Authority)));
    }
}
