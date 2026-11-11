using System;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Paging.Styles;

/// <summary>
/// The remote gives a continuation token in the page's JSON body, at a path the connection states, and reads it back from a query parameter
/// the connection names (nextPageToken/pageToken, next_cursor/cursor) (FDW-4938). No token there, null or empty, ends the read.
/// </summary>
[TypeOption(typeof(HttpPagingStyles), "ContinuationToken")]
public sealed class ContinuationTokenStyle : HttpPagingStyleBase
{
    /// <summary>Initializes a new instance of the <see cref="ContinuationTokenStyle"/> class.</summary>
    public ContinuationTokenStyle()
        : base(
            "ContinuationToken",
            "Continuation token",
            "The remote gives a token in the page's JSON body and reads it back from a query parameter, both named by the connection",
            [HttpPagingProperties.TokenPath, HttpPagingProperties.TokenParameter, HttpPagingProperties.PageSizeParameter, HttpPagingProperties.PageSize, HttpPagingProperties.MaxPages],
            [HttpPagingProperties.TokenPath, HttpPagingProperties.TokenParameter])
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

        return GenericResult<HttpPagingPlan>.Success(new Tokens(
            Name, settings, pageSize.Value, maxPages.Value, window,
            Stated(settings, HttpPagingProperties.TokenPath)!, Stated(settings, HttpPagingProperties.TokenParameter)!));
    }

    /// <inheritdoc/>
    protected override IGenericResult<HttpPagingWindow> CreateWindow(HttpPagingSettings settings, int skip, int? take)
        => SizedFromTheStart(settings, skip, take);

    private sealed class Tokens(string style, HttpPagingSettings settings, int pageSize, int maxPages, HttpReadWindow window, string path, string parameter)
        : LinkedPlan(style, settings, pageSize, maxPages, window)
    {
        public override bool ReadsBody => true;

        protected override IGenericResult<Uri?> NextPage(HttpPageOutcome outcome)
        {
            var token = JsonBodyPath.Text(ConnectionName, Logger, outcome.Page, outcome.Body, path);
            if (token.IsFailure) return token.ToNewResult<Uri?>();
            return string.IsNullOrWhiteSpace(token.Value)
                ? GenericResult<Uri?>.Success(null)
                : GenericResult<Uri?>.Success(HttpQuery.With(outcome.Request, (parameter, token.Value)));
        }
    }
}
