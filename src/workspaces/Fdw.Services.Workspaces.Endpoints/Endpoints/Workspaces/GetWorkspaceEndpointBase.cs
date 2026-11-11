using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Reads one workspace with its members, resources and relationships.</summary>
public abstract class GetWorkspaceEndpointBase : CrudGetEndpointBase<WorkspaceNameRequest, WorkspaceDetailResponse>
{
    private readonly IWorkspaceConfigurationProvider _provider;
    private readonly IWorkspaceAccessPolicy _access;

    /// <summary>Initializes a new instance of the <see cref="GetWorkspaceEndpointBase"/> class.</summary>
    protected GetWorkspaceEndpointBase(ILogger<GetWorkspaceEndpointBase> logger, IWorkspaceConfigurationProvider provider, IWorkspaceAccessPolicy access) : base(logger)
    {
        _provider = provider;
        _access = access;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>Viewing a workspace is decided by <see cref="IWorkspaceAccessPolicy.MayView"/>, not by a platform permission alone (FDW-936).</remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string GetResourceIdentifier(WorkspaceNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceDetailResponse?>> FindByIdentifier(
        WorkspaceNameRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<WorkspaceDetailResponse?>();
        if (result.Value is null) return GenericResult<WorkspaceDetailResponse?>.Success(null);

        // A workspace the caller may not view answers exactly as an absent one (the base's 404). The only
        // other failure is an access decision that could not be made, which is passed on (FDW-937).
        var permitted = await _access.MayView(result.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure)
        {
            return WorkspaceAccessRefusals.Hides(permitted)
                ? GenericResult<WorkspaceDetailResponse?>.Success(null)
                : permitted.ToNewResult<WorkspaceDetailResponse?>();
        }

        var ownership = await WorkspaceOwnershipReader.Read(Resolve<IOrganizationProvider>(), result.Value, Logger, ct).ConfigureAwait(false);
        if (ownership.IsFailure) return ownership.ToNewResult<WorkspaceDetailResponse?>();

        // The variations a workspace owns are data sets it owns (FDW-3638), so they are among its resources beside the ones attached.
        var owned = await DataSetVariationQueries.OwnedBy(Resolve<IDataSetVariationConfigurationProvider>(), result.Value.Id, ct).ConfigureAwait(false);
        if (owned.IsFailure) return owned.ToNewResult<WorkspaceDetailResponse?>();

        var detail = WorkspaceResponseMapper.ToDetail(result.Value, ownership.Value!);
        foreach (var variation in owned.Value!)
        {
            detail.Resources.Add(new WorkspaceResourceDto
            {
                Id = variation.Id,
                ResourceType = MapKinds.DataSet,
                ResourceId = variation.Id,
                Relationship = WorkspaceResourceRelationships.ByName("Owns").Name,
            });
        }

        return GenericResult<WorkspaceDetailResponse?>.Success(detail);
    }
}
