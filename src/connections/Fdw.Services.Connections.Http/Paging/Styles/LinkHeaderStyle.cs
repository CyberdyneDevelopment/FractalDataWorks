using System;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Paging.Styles;

/// <summary>
/// The remote points to the next page in a <c>Link</c> response header with <c>rel="next"</c> (RFC 8288; GitHub, many REST APIs) (FDW-4938).
/// No link marked next, or one with an empty target, ends the read.
/// </summary>
[TypeOption(typeof(HttpPagingStyles), "LinkHeader")]
public sealed class LinkHeaderStyle : HttpPagingStyleBase
{
    /// <summary>Initializes a new instance of the <see cref="LinkHeaderStyle"/> class.</summary>
    public LinkHeaderStyle()
        : base(
            "LinkHeader",
            "Link header",
            "The remote points to the next page in a Link response header marked rel=\"next\"",
            [HttpPagingProperties.PageSizeParameter, HttpPagingProperties.PageSize, HttpPagingProperties.MaxPages],
            [])
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

        return GenericResult<HttpPagingPlan>.Success(new Linked(Name, settings, pageSize.Value, maxPages.Value, window));
    }

    /// <inheritdoc/>
    protected override IGenericResult<HttpPagingWindow> CreateWindow(HttpPagingSettings settings, int skip, int? take)
        => SizedFromTheStart(settings, skip, take);

    private sealed class Linked(string style, HttpPagingSettings settings, int pageSize, int maxPages, HttpReadWindow window)
        : LinkedPlan(style, settings, pageSize, maxPages, window)
    {
        protected override IGenericResult<Uri?> NextPage(HttpPageOutcome outcome)
            => LinkHeader.Target(outcome.Headers, "next") is { Length: > 0 } link
                ? Follow(outcome, link)
                : GenericResult<Uri?>.Success(null);
    }
}
