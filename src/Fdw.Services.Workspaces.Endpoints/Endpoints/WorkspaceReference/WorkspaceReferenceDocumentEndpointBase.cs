using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Results;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// The API reference of one workspace: <c>GET /openapi/data/{WorkspaceName}</c>.
/// </summary>
/// <remarks>
/// The same document as the data reference, cut to one workspace. A workspace that does not exist and
/// one the caller may not read both answer 404 <c>WorkspaceNotReadable</c>, as the data route does, so
/// the reference never confirms that a workspace exists. The document also admits only a caller the
/// <c>WorkspaceReference</c> page admits, by the policies the host configured for that page when it
/// configured any (<see cref="ApiReferencePagePolicies"/>).
/// </remarks>
public abstract class WorkspaceReferenceDocumentEndpointBase : WorkspaceDocumentEndpointBase<WorkspaceReferenceRequest>
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceReferenceDocumentEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="systemRoles">Decides whether the caller is a platform admin.</param>
    /// <param name="workspaces">Resolves the workspace with its members and resources.</param>
    /// <param name="access">Decides whether the caller may read it.</param>
    /// <param name="readService">Resolves the data sets and variations a workspace exposes, with their fields.</param>
    protected WorkspaceReferenceDocumentEndpointBase(
        ILogger<ApiReferenceDocumentEndpointBase<WorkspaceReferenceRequest>> logger,
        ISystemRoleConfiguration systemRoles,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IWorkspaceReadService readService)
        : base(logger, systemRoles, workspaces, access, readService)
    {
    }

    /// <inheritdoc />
    protected override string? PageName => "WorkspaceReference";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/openapi/data/{WorkspaceName}");
        // Signed in is the only gate: MayRead decides, so a member without workspaces:query still
        // gets the document for a workspace they may query (FDW-936).
        Description(builder => builder.ExcludeFromDescription());
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Filter(
        ApiReferenceDocument document,
        ApiReferenceCaller caller,
        WorkspaceReferenceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var workspace = await Workspaces.Get(request.WorkspaceName, cancellationToken).ConfigureAwait(false);
        if (workspace.IsFailure)
            return workspace;
        if (workspace.Value is not { } found)
            return NotReadable(request.WorkspaceName);

        var permitted = await Access.MayRead(found, cancellationToken).ConfigureAwait(false);
        if (WorkspaceAccessRefusals.IsUndetermined(permitted))
            return permitted;
        if (permitted.IsFailure)
            return NotReadable(request.WorkspaceName);

        return await Describe(document, [found], found.Name, cancellationToken).ConfigureAwait(false);
    }

    // The policy has already logged why; the caller is told only what an absent workspace would tell them.
    private IGenericResult NotReadable(string workspaceName)
        => GenericResult.Failure(
            WorkspacesResultCodes.ByName("WorkspaceNotReadable"), Logger, ResultDetails.Create("name", workspaceName));
}
