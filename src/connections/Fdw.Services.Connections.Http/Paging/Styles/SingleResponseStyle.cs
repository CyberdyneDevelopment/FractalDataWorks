using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Paging.Styles;

/// <summary>
/// The remote does not page: one response holds every row (FDW-4938). Stated explicitly, so a remote that does not page is told apart from
/// a connection that says nothing about paging, which is refused. A query's window is cut from that one response by the reader, since the
/// remote cannot be asked for it.
/// </summary>
[TypeOption(typeof(HttpPagingStyles), "SingleResponse")]
public sealed class SingleResponseStyle : HttpPagingStyleBase
{
    private static readonly IReadOnlyDictionary<string, object> NoDefaults = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="SingleResponseStyle"/> class.</summary>
    public SingleResponseStyle()
        : base("SingleResponse", "Single response", "The remote does not page: one response holds every row", [], [])
    {
    }

    /// <inheritdoc/>
    public override bool Pages => false;

    /// <inheritdoc/>
    /// <remarks>It asks for no page size and reads one page, so it declares no tuning value.</remarks>
    public override IReadOnlyDictionary<string, object> DeclaredDefaults => NoDefaults;

    /// <inheritdoc/>
    protected override IGenericResult<HttpPagingPlan> CreatePlan(HttpPagingSettings settings, HttpReadWindow window)
        => GenericResult<HttpPagingPlan>.Success(new OneResponse(Name, settings, window));

    /// <inheritdoc/>
    protected override IGenericResult<HttpPagingWindow> CreateWindow(HttpPagingSettings settings, int skip, int? take)
        => GenericResult<HttpPagingWindow>.Success(new HttpPagingWindow([], skip, take));

    private sealed class OneResponse(string style, HttpPagingSettings settings, HttpReadWindow window)
        : HttpPagingPlan(style, settings, pageSize: 0, maxPages: 1, window)
    {
        public override int SkipHere { get; } = window.Skip;

        public override Uri FirstPage(Uri request) => request;

        protected override IGenericResult<Uri?> NextPage(HttpPageOutcome outcome) => GenericResult<Uri?>.Success(null);
    }
}
