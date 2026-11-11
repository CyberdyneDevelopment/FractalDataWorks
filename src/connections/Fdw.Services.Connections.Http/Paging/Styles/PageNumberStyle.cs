using System;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.Http.Logging;

namespace Fdw.Services.Connections.Http.Paging.Styles;

/// <summary>
/// The remote pages by a page number and a page size in the query string (page/per_page, page[number]/page[size]) (FDW-4938). The connection
/// states both parameter names and the number of the remote's first page. An empty page is the end.
/// </summary>
/// <remarks>
/// A window can be asked for only when it starts on a page boundary: a query that skips a number of rows that is not a whole number of pages
/// is refused, never rounded to the page it falls in (which returned rows the query did not ask for).
/// </remarks>
[TypeOption(typeof(HttpPagingStyles), "PageNumber")]
public sealed class PageNumberStyle : HttpPagingStyleBase
{
    /// <summary>Initializes a new instance of the <see cref="PageNumberStyle"/> class.</summary>
    public PageNumberStyle()
        : base(
            "PageNumber",
            "Page number",
            "The remote pages by a page number and a page size, in query parameters the connection names, counting pages from the first page number it states",
            [HttpPagingProperties.PageNumberParameter, HttpPagingProperties.PageSizeParameter, HttpPagingProperties.FirstPageNumber, HttpPagingProperties.PageSize, HttpPagingProperties.MaxPages],
            [HttpPagingProperties.PageNumberParameter, HttpPagingProperties.PageSizeParameter, HttpPagingProperties.FirstPageNumber])
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<HttpPagingPlan> CreatePlan(HttpPagingSettings settings, HttpReadWindow window)
    {
        var pageSize = WholeNumber(settings, HttpPagingProperties.PageSize, 1);
        if (pageSize.IsFailure) return pageSize.ToNewResult<HttpPagingPlan>();
        var maxPages = WholeNumber(settings, HttpPagingProperties.MaxPages, 1);
        if (maxPages.IsFailure) return maxPages.ToNewResult<HttpPagingPlan>();
        var first = WholeNumber(settings, HttpPagingProperties.FirstPageNumber, 0);
        if (first.IsFailure) return first.ToNewResult<HttpPagingPlan>();
        var start = StartPage(settings, first.Value, window.Skip, pageSize.Value);
        if (start.IsFailure) return start.ToNewResult<HttpPagingPlan>();

        return GenericResult<HttpPagingPlan>.Success(new Numbered(
            Name, settings, pageSize.Value, maxPages.Value, window, start.Value,
            Stated(settings, HttpPagingProperties.PageNumberParameter)!, Stated(settings, HttpPagingProperties.PageSizeParameter)!));
    }

    /// <inheritdoc/>
    protected override IGenericResult<HttpPagingWindow> CreateWindow(HttpPagingSettings settings, int skip, int? take)
    {
        if (take is not int size || size < 1)
            return GenericResult<HttpPagingWindow>.Failure(HttpPagingLog.PagingWindowNotAddressable(
                settings.Logger, settings.ConnectionName, Name, skip, "it asks for a page by its size, and the query takes no number of rows"));

        var first = WholeNumber(settings, HttpPagingProperties.FirstPageNumber, 0);
        if (first.IsFailure) return first.ToNewResult<HttpPagingWindow>();
        var start = StartPage(settings, first.Value, skip, size);
        if (start.IsFailure) return start.ToNewResult<HttpPagingWindow>();

        return GenericResult<HttpPagingWindow>.Success(new HttpPagingWindow(
            [
                (Stated(settings, HttpPagingProperties.PageNumberParameter)!, start.Value.ToString(CultureInfo.InvariantCulture)),
                (Stated(settings, HttpPagingProperties.PageSizeParameter)!, size.ToString(CultureInfo.InvariantCulture)),
            ],
            0,
            take));
    }

    private IGenericResult<long> StartPage(HttpPagingSettings settings, int firstPage, int skip, int pageSize)
        => skip % pageSize == 0
            ? GenericResult<long>.Success(firstPage + ((long)skip / pageSize))
            : GenericResult<long>.Failure(HttpPagingLog.PagingWindowNotAddressable(
                settings.Logger, settings.ConnectionName, Name, skip,
                $"it is not a whole number of pages of {pageSize}, and a page number cannot start inside a page"));

    private sealed class Numbered(string style, HttpPagingSettings settings, int pageSize, int maxPages, HttpReadWindow window, long startPage, string numberName, string sizeName)
        : HttpPagingPlan(style, settings, pageSize, maxPages, window)
    {
        private long _page = startPage;

        public override Uri FirstPage(Uri request) => At(request);

        protected override IGenericResult<Uri?> NextPage(HttpPageOutcome outcome)
        {
            if (outcome.Rows == 0)
                return GenericResult<Uri?>.Success(null);

            _page++;
            return GenericResult<Uri?>.Success(At(outcome.Request));
        }

        private Uri At(Uri request) => HttpQuery.With(
            request,
            (numberName, _page.ToString(CultureInfo.InvariantCulture)),
            (sizeName, PageSize.ToString(CultureInfo.InvariantCulture)));
    }
}
