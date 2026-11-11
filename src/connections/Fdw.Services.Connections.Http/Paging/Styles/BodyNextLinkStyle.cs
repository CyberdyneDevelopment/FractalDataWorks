using System;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Paging.Styles;

/// <summary>
/// The remote points to the next page with an address in the page's JSON body, at a path the connection states (<c>@odata.nextLink</c>,
/// <c>links.next</c>, <c>next</c>) (FDW-4938). No address there, null or empty, ends the read: remotes write each of these for "no next page".
/// </summary>
[TypeOption(typeof(HttpPagingStyles), "BodyNextLink")]
public sealed class BodyNextLinkStyle : HttpPagingStyleBase
{
    /// <summary>Initializes a new instance of the <see cref="BodyNextLinkStyle"/> class.</summary>
    public BodyNextLinkStyle()
        : base(
            "BodyNextLink",
            "Next link in the body",
            "The remote points to the next page with an address in the page's JSON body, at a path the connection names",
            [HttpPagingProperties.NextLinkPath, HttpPagingProperties.PageSizeParameter, HttpPagingProperties.PageSize, HttpPagingProperties.MaxPages],
            [HttpPagingProperties.NextLinkPath])
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<HttpPagingPlan> CreatePlan(HttpPagingSettings settings, HttpReadWindow window)
    {
        var fromStart = FromTheStart(settings, window.Skip);
        if (fromStart.IsFailure) return fromStart.ToNewResult<HttpPagingPlan>();
        var pageSize = SizeAskedFor(settings);
        if (pageSize.IsFailure) return pageSize.ToNewResult<HttpPagingPlan>();
        var maxPages = WholeNumber(settings, HttpPagingProperties.MaxPages, 1);
        if (maxPages.IsFailure) return maxPages.ToNewResult<HttpPagingPlan>();

        return GenericResult<HttpPagingPlan>.Success(new BodyLinked(
            Name, settings, pageSize.Value, maxPages.Value, window, Stated(settings, HttpPagingProperties.NextLinkPath)!));
    }

    /// <inheritdoc/>
    protected override IGenericResult<HttpPagingWindow> CreateWindow(HttpPagingSettings settings, int skip, int? take)
        => SizedFromTheStart(settings, skip, take);

    private sealed class BodyLinked(string style, HttpPagingSettings settings, int pageSize, int maxPages, HttpReadWindow window, string path)
        : LinkedPlan(style, settings, pageSize, maxPages, window)
    {
        public override bool ReadsBody => true;

        protected override IGenericResult<Uri?> NextPage(HttpPageOutcome outcome)
        {
            var link = JsonBodyPath.Text(ConnectionName, Logger, outcome.Page, outcome.Body, path);
            if (link.IsFailure) return link.ToNewResult<Uri?>();
            return string.IsNullOrWhiteSpace(link.Value) ? GenericResult<Uri?>.Success(null) : Follow(outcome, link.Value);
        }
    }
}
