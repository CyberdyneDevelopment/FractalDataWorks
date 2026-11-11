using System;
using System.Globalization;

namespace Fdw.Services.Connections.Http.Paging.Styles;

/// <summary>
/// The part every style that follows the remote's own pointer shares (LinkHeader, BodyNextLink, ContinuationToken; FDW-4938): the first page
/// asks for the page size when the connection names its parameter, and every later page is where the remote points.
/// </summary>
/// <remarks>Why a short page does not end these reads: the remote decides where its pages break, so only a missing pointer says it is done.</remarks>
public abstract class LinkedPlan : HttpPagingPlan
{
    private readonly string? _sizeName;

    /// <summary>Initializes a new instance of the <see cref="LinkedPlan"/> class.</summary>
    /// <param name="style">The style that built the plan.</param>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="pageSize">The page size asked for; 0 when the connection names no parameter for it.</param>
    /// <param name="maxPages">The most pages the read may take.</param>
    /// <param name="window">The rows the read returns; it starts at the first row.</param>
    protected LinkedPlan(string style, HttpPagingSettings settings, int pageSize, int maxPages, HttpReadWindow window)
        : base(style, settings, pageSize, maxPages, window)
    {
        _sizeName = settings.Values.TryGetValue(HttpPagingProperties.PageSizeParameter, out var name) && !string.IsNullOrWhiteSpace(name) ? name.Trim() : null;
    }

    /// <inheritdoc/>
    public override Uri FirstPage(Uri request)
        => _sizeName is null || PageSize == 0 ? request : HttpQuery.With(request, (_sizeName, PageSize.ToString(CultureInfo.InvariantCulture)));
}
