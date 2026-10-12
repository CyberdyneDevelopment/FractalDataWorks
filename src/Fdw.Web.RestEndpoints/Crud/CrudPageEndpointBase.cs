using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Web.Http.Abstractions.EndPoints;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Base class for list endpoints over configuration a domain provider pages itself: one page is read,
/// and each item is composed and written as the response is written.
/// </summary>
/// <typeparam name="TItem">The configuration the provider composes.</typeparam>
/// <typeparam name="TSummary">The summary written for each item.</typeparam>
/// <remarks>
/// <see cref="CrudListEndpointBase{TSummary}"/> loads the whole list and cuts the page in memory,
/// which composes every member on every request. Here the provider reads only the page's rows, in
/// name order, with one row past the page to tell whether another follows, and composes each member
/// only when the response reaches it.
/// <para>
/// Everything checked before the first byte (the paging values, the page's rows) still fails with a
/// real status code. A member that fails after the first byte ends the page; the failure is written as
/// the response's <c>error</c> property, after the items. See <see cref="StreamedPageResponse{T}"/>.
/// </para>
/// </remarks>
public abstract class CrudPageEndpointBase<TItem, TSummary> : EndpointWithoutRequest<StreamedPageResponse<TSummary>>
    where TSummary : class
{
    /// <summary>Initializes a new instance of the <see cref="CrudPageEndpointBase{TItem, TSummary}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected CrudPageEndpointBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Gets the plural resource name used for routing and policy generation (e.g., "datastores").</summary>
    protected abstract string ResourceName { get; }

    /// <summary>Gets the documentation tag this endpoint appears under.</summary>
    /// <remarks>Derived from the resource, like the route and the policy beside it.</remarks>
    protected virtual string EndpointTag => ResourceName;

    /// <summary>Gets the read policy for this endpoint. Defaults to "{ResourceName}:read".</summary>
    protected virtual string ReadPolicy => $"{ResourceName}:read";

    /// <summary>
    /// Gets whether the resource this endpoint acts on decides who may use it, instead of a platform permission.
    /// </summary>
    /// <remarks>
    /// When true the endpoint requires only a signed-in caller and asks the resource's own access
    /// policy, as <see cref="CrudListEndpointBase{TSummary}"/> does (FDW-936).
    /// </remarks>
    protected virtual bool ResourceDecidesAccess => false;

    /// <summary>Gets the route for this endpoint. Defaults to "/{ResourceName}".</summary>
    protected virtual string Route => $"/{ResourceName}";

    /// <summary>Gets the endpoint summary for OpenAPI documentation.</summary>
    protected virtual string EndpointSummary => $"List {ResourceName}";

    /// <summary>Gets the endpoint description for OpenAPI documentation.</summary>
    protected virtual string EndpointDescription =>
        $"Streams one page of {ResourceName} in name order, sized by ?skip= and ?take=. " +
        "hasMore says whether another page follows; error, written after the items, says why a page ended early.";

    /// <summary>Gets the logger instance.</summary>
    protected new ILogger Logger { get; }

    /// <summary>
    /// Gets whether this list reads <c>?q=</c>, <c>?match=</c> and <c>?total=true</c> from the request (FDW-1194).
    /// </summary>
    /// <remarks>
    /// False keeps a list as it was: those parameters are not read, so a request that carries them is answered as it was
    /// before they existed. A list that sets this true builds its query from <see cref="Search"/>.
    /// </remarks>
    protected virtual bool Searchable => false;

    /// <summary>Gets what this request asked of the list beyond its page; nothing when the list is not <see cref="Searchable"/>.</summary>
    /// <remarks>
    /// An endpoint serves one request, so what is read here is this request's. It is read before the page is loaded;
    /// settable so a test can state a search without a running host.
    /// </remarks>
    protected ListSearch Search { get; set; } = ListSearch.None;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get(Route);
#if DEVELOP
        AllowAnonymous();
#else
        if (!ResourceDecidesAccess)
            Policies(ReadPolicy);
#endif
        // Why the metadata: response-buffering middleware would otherwise hold the whole page to set a
        // Content-Length, composing every member before the first byte with no outward sign.
        Options(builder => builder.WithMetadata(StreamingResponseMetadata.Instance));

        Summary(s =>
        {
            s.Summary = EndpointSummary;
            s.Description = EndpointDescription;
        });

        Description(x => x.WithTags(EndpointTag));

        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup such as tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var paging = PaginationQuery.Parse(HttpContext, Logger);
            if (paging.IsFailure)
            {
                await SendErrorResponse(paging, ct).ConfigureAwait(false);
                return;
            }

            if (Searchable)
            {
                var search = ListSearch.Parse(HttpContext, Logger);
                if (search.IsFailure)
                {
                    await SendErrorResponse(search, ct).ConfigureAwait(false);
                    return;
                }

                Search = search.Value!;
            }

            EndpointLogger.PaginatedListRequest(Logger, GetType().Name, paging.Value!.Skip, paging.Value.Take);

            var page = await LoadPage(paging.Value, ct).ConfigureAwait(false);
            if (page.IsFailure)
            {
                await SendErrorResponse(page, ct).ConfigureAwait(false);
                return;
            }

            var loaded = page.Value!;
            IGenericResult ended = GenericResult.Success();
            var response = new StreamedPageResponse<TSummary>(
                loaded.Skip,
                loaded.Take,
                loaded.HasMore,
                Summaries(loaded, failure => ended = failure, ct),
                () => Error(ended.IsFailure ? ended : loaded.Outcome),
                loaded.Total);

            await Send.ResultAsync(TypedResults.Json(response)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var msg = EndpointLogger.EndpointError(Logger, ex, GetType().Name);

            // Once the first byte is out the status code is fixed; a cut connection is what tells the
            // client the page is incomplete, where a closed body would read as a whole one.
            if (HttpContext.Response.HasStarted)
            {
                HttpContext.Abort();
                return;
            }

            await SendErrorResponse(GenericResult.Failure(msg), ct).ConfigureAwait(false);
        }
    }

    /// <summary>Reads one page of items from the provider.</summary>
    /// <param name="paging">The page the caller asked for, sized by the pagination settings.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The page, or a structured failure when its rows could not be read.</returns>
    protected abstract Task<IGenericResult<ConfigurationPage<TItem>>> LoadPage(PaginatedListRequest paging, CancellationToken ct);

    /// <summary>Produces the summary written for one item.</summary>
    /// <param name="item">The composed item.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The summary, or a structured failure, which ends the page.</returns>
    protected abstract Task<IGenericResult<TSummary>> ToSummary(TItem item, CancellationToken ct);

    /// <summary>Says whether an item is written to the page.</summary>
    /// <param name="item">The composed item.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True to write the item.</returns>
    /// <remarks>
    /// An excluded item still counts toward the page's size: a page that excludes one carries one item
    /// fewer than it could, and the next page still starts at skip plus take. Asynchronous because what
    /// decides it may be a read of its own, such as an access policy asking whether the caller may see it.
    /// </remarks>
    protected virtual ValueTask<bool> Include(TItem item, CancellationToken ct) => ValueTask.FromResult(true);

    /// <summary>Says whether an item is written to the page, or why that could not be decided.</summary>
    /// <param name="item">The composed item.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Success carrying whether to write the item; a failure ends the page with it.</returns>
    /// <remarks>
    /// For a decision that can fail rather than merely say no -- an access policy whose own reads are
    /// down. A decision that could not be made must not be answered as "leave it out": the page would
    /// report an outage as fewer items. A failure ends the page exactly as a failed summary does
    /// (FDW-937). The default asks <see cref="Include"/>, which cannot fail.
    /// </remarks>
    protected virtual async ValueTask<IGenericResult<bool>> Admits(TItem item, CancellationToken ct)
        => GenericResult<bool>.Success(await Include(item, ct).ConfigureAwait(false));

    /// <summary>Sends an error response for a failure found before the page started.</summary>
    /// <param name="result">The failure.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the send.</returns>
    protected virtual Task SendErrorResponse(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        return Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode));
    }

    private async IAsyncEnumerable<TSummary> Summaries(
        ConfigurationPage<TItem> page,
        Action<IGenericResult> fail,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var sent = 0;
        await foreach (var item in page.Items.WithCancellation(ct).ConfigureAwait(false))
        {
            var admitted = await Admits(item, ct).ConfigureAwait(false);
            if (admitted.IsFailure)
            {
                fail(admitted);
                EndpointLogger.StreamedPageEnded(Logger, GetType().Name, sent, admitted.CurrentMessage);
                yield break;
            }

            if (!admitted.Value) continue;

            var summary = await ToSummary(item, ct).ConfigureAwait(false);
            if (summary.IsSuccess)
            {
                sent++;
                yield return summary.Value!;
                continue;
            }

            fail(summary);
            EndpointLogger.StreamedPageEnded(Logger, GetType().Name, sent, summary.CurrentMessage);
            yield break;
        }

        if (page.Outcome.IsFailure)
            EndpointLogger.StreamedPageEnded(Logger, GetType().Name, sent, page.Outcome.CurrentMessage);
        else
            EndpointLogger.StreamedPageSent(Logger, GetType().Name, page.Skip, sent, page.HasMore);
    }

    private Microsoft.AspNetCore.Mvc.ProblemDetails? Error(IGenericResult outcome)
        => outcome.IsFailure ? ResultHttpStatusMapper.Map(outcome, HttpContext).Response : null;
}
