using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Web.Endpoints;
using Fdw.Web.Endpoints.Contracts;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Audit;

/// <summary>
/// Lists administrative changes, newest first, within the tenant scope the caller reaches: a tenant administrator sees their
/// tenant's changes, the platform sees every tenant's.
/// </summary>
/// <remarks>
/// The route always requires <c>audit:read</c>. It has no anonymous branch for Develop builds, because an audit trail that
/// any caller can read is not one.
/// </remarks>
public abstract class ListAdministrativeChangesEndpointBase : Endpoint<ListAdministrativeChangesRequest, PaginatedResponse<AdministrativeChangeResponse>>
{
    private readonly IAdministrativeAudit _audit;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListAdministrativeChangesEndpointBase"/> class.
    /// </summary>
    /// <param name="logger">The logger of the concrete endpoint.</param>
    /// <param name="audit">Reads the recorded changes.</param>
    protected ListAdministrativeChangesEndpointBase(ILogger logger, IAdministrativeAudit audit)
    {
        EndpointLogger = logger;
        _audit = audit;
    }

    /// <summary>Gets the logger of the concrete endpoint.</summary>
    protected ILogger EndpointLogger { get; }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/audit/changes");
        Policies("audit:read");
        Summary(s =>
        {
            s.Summary = "List administrative changes";
            s.Description = "Returns a page of administrative changes, newest first, with who made each, what changed, at which level, the old and new value, and when. Filter by person, tenant, organization and time.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ListAdministrativeChangesRequest req, CancellationToken ct)
    {
        var paging = PaginationQuery.ResolvePage(HttpContext, EndpointLogger, req.Page, req.PageSize);
        if (!paging.IsSuccess || paging.Value is not { } page)
        {
            await SendFailure(paging).ConfigureAwait(false);
            return;
        }

        OnListingChanges(req.Page, page.Take);
        var read = await _audit.Read(
            new AdministrativeChangeFilter(req.Page, page.Take, req.ActorUserId, req.TenantId, req.OrganizationId, req.From, req.To),
            ct).ConfigureAwait(false);
        if (!read.IsSuccess || read.Value is not { } changes)
        {
            await SendFailure(read).ConfigureAwait(false);
            return;
        }

        OnChangesListed(changes.Items.Count);
        await Send.OkAsync(new PaginatedResponse<AdministrativeChangeResponse>
        {
            Items = changes.Items.Select(AdministrativeChangeResponse.Of).ToList(),
            Page = changes.Page,
            PageSize = changes.PageSize,
            TotalCount = (int)changes.TotalCount
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Called when a page of changes is about to be read.
    /// </summary>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The page size.</param>
    protected virtual void OnListingChanges(int page, int pageSize)
    {
    }

    /// <summary>
    /// Called when a page of changes has been read.
    /// </summary>
    /// <param name="count">How many changes the page holds.</param>
    protected virtual void OnChangesListed(int count)
    {
    }

    private async Task SendFailure(IGenericResult failed)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(failed, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}
