using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// The API reference of every workspace the caller may read: <c>GET /openapi/data</c>.
/// </summary>
/// <remarks>
/// Each workspace is a group, and each data set attached to it is one operation. Gated like the data
/// route itself: signed in to open it, then <see cref="IWorkspaceAccessPolicy.MayRead"/> for each
/// workspace it lists (FDW-937). The document also admits only a caller the <c>DataReference</c> page
/// admits, by the policies the host configured for that page when it configured any
/// (<see cref="ApiReferencePagePolicies"/>).
/// </remarks>
public abstract class DataReferenceDocumentEndpointBase : WorkspaceDocumentEndpointBase<EmptyRequest>
{
    /// <summary>Initializes a new instance of the <see cref="DataReferenceDocumentEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="systemRoles">Decides whether the caller is a platform admin.</param>
    /// <param name="workspaces">Resolves the workspaces with their members and resources.</param>
    /// <param name="access">Decides whether the caller may read a workspace.</param>
    /// <param name="dataSets">Resolves the attached data sets and their fields.</param>
    protected DataReferenceDocumentEndpointBase(
        ILogger<ApiReferenceDocumentEndpointBase<EmptyRequest>> logger,
        ISystemRoleConfiguration systemRoles,
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        IDataSetConfigurationProvider dataSets)
        : base(logger, systemRoles, workspaces, access, dataSets)
    {
    }

    /// <inheritdoc />
    protected override string? PageName => "DataReference";

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/openapi/data");
        // Signed in is the only gate: the document lists what MayRead lets this caller query, so a
        // member without workspaces:query still gets their own workspaces (FDW-936).
        Description(builder => builder.ExcludeFromDescription());
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Filter(
        ApiReferenceDocument document,
        ApiReferenceCaller caller,
        EmptyRequest request,
        CancellationToken cancellationToken)
    {
        var readable = await ReadableWorkspaces.Get(Workspaces, Access, cancellationToken).ConfigureAwait(false);
        if (readable.IsFailure || readable.Value is not { } workspaces)
            return readable;

        return await Describe(document, workspaces, "Data", cancellationToken).ConfigureAwait(false);
    }
}
