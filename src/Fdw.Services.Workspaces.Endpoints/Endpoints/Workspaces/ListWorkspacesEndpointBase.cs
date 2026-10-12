using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using System;
using Fdw.Services.Abstractions;
using Fdw.Web.RestEndpoints.Models;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Lists the workspaces visible to the caller, one page at a time (FDW-951).</summary>
public abstract class ListWorkspacesEndpointBase : CrudPageEndpointBase<IWorkspaceImplementationConfiguration, WorkspaceSummaryResponse>
{
    private readonly IWorkspaceConfigurationProvider _provider;
    private readonly IWorkspaceAccessPolicy _access;

    /// <inheritdoc />
    protected ListWorkspacesEndpointBase(ILogger<ListWorkspacesEndpointBase> logger, IWorkspaceConfigurationProvider provider, IWorkspaceAccessPolicy access) : base(logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _access = access ?? throw new ArgumentNullException(nameof(access));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>
    /// Everyone signed in may ask; what comes back is decided per workspace by
    /// <see cref="IWorkspaceAccessPolicy.MayView"/>: a workspace is listed for exactly the callers who
    /// may view it (FDW-937) -- every one for a holder of <c>workspaces:read</c>, and otherwise those
    /// the caller owns, is a member of, holds <c>workspaces/{id}:view</c> for, or that are Open.
    /// </remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    /// <remarks>
    /// One level down: the access policy reads each workspace's members, and nothing below them.
    /// </remarks>
    protected override Task<IGenericResult<ConfigurationPage<IWorkspaceImplementationConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
        => _provider.Get(paging.Skip, paging.Take, ConfigurationLevels.Children, ct);

    /// <summary>Leaves out a workspace the caller may not view.</summary>
    /// <param name="item">The workspace.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Success carrying whether the caller may view it; the policy's failure when it could not decide.</returns>
    /// <remarks>
    /// Listing is having it (FDW-937): a workspace is listed for exactly the callers who may view it. One
    /// the caller may not view is left out, not reported: the policy has already logged why, and a
    /// listing that named it would be the very disclosure the refusal prevents. A page can therefore
    /// carry fewer workspaces than its size. A decision that could not be made ends the page instead,
    /// so an outage is never answered as a shorter list.
    /// </remarks>
    protected override async ValueTask<IGenericResult<bool>> Admits(IWorkspaceImplementationConfiguration item, CancellationToken ct)
    {
        var views = await _access.MayView(item, ct).ConfigureAwait(false);
        return WorkspaceAccessRefusals.IsUndetermined(views)
            ? views.ToNewResult<bool>()
            : GenericResult<bool>.Success(views.IsSuccess);
    }

    /// <inheritdoc />
    /// <remarks>Each item carries the tenant that owns it and the organization that names it as its default (FDW-1101).</remarks>
    protected override async Task<IGenericResult<WorkspaceSummaryResponse>> ToSummary(IWorkspaceImplementationConfiguration item, CancellationToken ct)
    {
        var ownership = await WorkspaceOwnershipReader.Read(Resolve<IOrganizationProvider>(), item, Logger, ct).ConfigureAwait(false);
        return ownership.IsFailure
            ? ownership.ToNewResult<WorkspaceSummaryResponse>()
            : GenericResult<WorkspaceSummaryResponse>.Success(WorkspaceResponseMapper.ToSummary(item, ownership.Value!));
    }
}
