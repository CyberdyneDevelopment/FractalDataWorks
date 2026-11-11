using System;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Paging.Styles;

/// <summary>
/// The remote pages by an offset and a limit in the query string (offset/limit, $skip/$top, start/count) (FDW-4938). The connection states
/// both parameter names. Each page starts after the rows already read, and an empty page is the end.
/// </summary>
/// <remarks>
/// Why an empty page and not a short one ends the read: a remote that caps its pages below the size asked for returns a short first page,
/// and ending there would return a fraction of the set as the whole of it. The offset moves on by the rows each page actually held.
/// </remarks>
[TypeOption(typeof(HttpPagingStyles), "OffsetLimit")]
public sealed class OffsetLimitStyle : HttpPagingStyleBase
{
    /// <summary>Initializes a new instance of the <see cref="OffsetLimitStyle"/> class.</summary>
    public OffsetLimitStyle()
        : base(
            "OffsetLimit",
            "Offset and limit",
            "The remote pages by the rows to skip and the rows to return, in query parameters the connection names",
            [HttpPagingProperties.OffsetParameter, HttpPagingProperties.LimitParameter, HttpPagingProperties.PageSize, HttpPagingProperties.MaxPages],
            [HttpPagingProperties.OffsetParameter, HttpPagingProperties.LimitParameter])
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<HttpPagingPlan> CreatePlan(HttpPagingSettings settings, HttpReadWindow window)
    {
        var pageSize = WholeNumber(settings, HttpPagingProperties.PageSize, 1);
        if (pageSize.IsFailure) return pageSize.ToNewResult<HttpPagingPlan>();
        var maxPages = WholeNumber(settings, HttpPagingProperties.MaxPages, 1);
        if (maxPages.IsFailure) return maxPages.ToNewResult<HttpPagingPlan>();

        return GenericResult<HttpPagingPlan>.Success(new Offsets(
            Name, settings, pageSize.Value, maxPages.Value, window,
            Stated(settings, HttpPagingProperties.OffsetParameter)!, Stated(settings, HttpPagingProperties.LimitParameter)!));
    }

    /// <inheritdoc/>
    protected override IGenericResult<HttpPagingWindow> CreateWindow(HttpPagingSettings settings, int skip, int? take)
    {
        var offset = (Stated(settings, HttpPagingProperties.OffsetParameter)!, skip.ToString(CultureInfo.InvariantCulture));
        return GenericResult<HttpPagingWindow>.Success(take is int limit
            ? new HttpPagingWindow([offset, (Stated(settings, HttpPagingProperties.LimitParameter)!, limit.ToString(CultureInfo.InvariantCulture))], 0, take)
            : new HttpPagingWindow([offset], 0, take));
    }

    private sealed class Offsets(string style, HttpPagingSettings settings, int pageSize, int maxPages, HttpReadWindow window, string offsetName, string limitName)
        : HttpPagingPlan(style, settings, pageSize, maxPages, window)
    {
        private long _offset = window.Skip;

        public override Uri FirstPage(Uri request) => At(request);

        protected override IGenericResult<Uri?> NextPage(HttpPageOutcome outcome)
        {
            if (outcome.Rows == 0)
                return GenericResult<Uri?>.Success(null);

            _offset += outcome.Rows;
            return GenericResult<Uri?>.Success(At(outcome.Request));
        }

        private Uri At(Uri request) => HttpQuery.With(
            request,
            (offsetName, _offset.ToString(CultureInfo.InvariantCulture)),
            (limitName, PageSize.ToString(CultureInfo.InvariantCulture)));
    }
}
