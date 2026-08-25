using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for adding a container to an existing data store path.
/// POST datastores/{name}/containers
/// </summary>
public abstract class AddDataStoreContainerEndpointBase : CrudCreateEndpointBase<AddDataStoreContainerRequest, DataStoreContainerResponse>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected AddDataStoreContainerEndpointBase(DataStoreConfigurationProvider dataStoreProvider)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Gets the nested-resource route.</summary>
    protected override string Route => "/datastores/{Name}/containers";

    /// <summary>Returns the container name as the resource identifier for duplicate-check logging.</summary>
    protected override string GetResourceName(AddDataStoreContainerRequest request) => request.ContainerName;

    /// <summary>
    /// Existence and duplicate-container checks are delegated to the provider's AddContainer method.
    /// Always returns false so the CrudCreateEndpointBase lifecycle proceeds to Create.
    /// </summary>
    protected override Task<IGenericResult<bool>> CheckExists(AddDataStoreContainerRequest request, CancellationToken ct)
        => Task.FromResult(GenericResult<bool>.Success(false));

    /// <summary>Delegates persistence to DataStoreConfigurationProvider.AddContainer, which enforces
    /// store-exists, path-exists, and no-duplicate-name invariants before writing.</summary>
    protected override async Task<IGenericResult<DataStoreContainerResponse>> Create(AddDataStoreContainerRequest request, CancellationToken ct)
    {
        var container = new DataContainerConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = request.ContainerName,
            TypeId = request.ContainerType,
            // Why: format is config-driven and carried inline on the container config — the Format
            // discriminator selects the record-source factory; RecordSelector/Flatten ride alongside.
            Format = request.Format,
            RecordSelector = request.RecordSelector,
            FlattenNestedObjects = request.FlattenNestedObjects,
            FlattenSeparator = request.FlattenSeparator,
            // Why: without caller-supplied fields the container persists with zero data.DataContainerField
            // rows and bulk-insert later fails "Container X has no insertable fields" (FDW-548). Id and the
            // DataContainerId FK are left unset — DefaultConfigurationProvider.Save's cascade mints each
            // child's Id and stamps the FK, mirroring CreateDataSetEndpointBase.MapFields.
            Fields = request.Fields.Select(f => new DataContainerFieldConfiguration
            {
                Name = f.Name,
                DataType = f.DataType,
                IsNullable = f.IsNullable,
                Ordinal = f.Ordinal,
                IsSystemProvided = f.IsSystemProvided,
                VisibilityId = f.VisibilityId,
                Description = f.Description,
            }).ToList(),
        };

        var addResult = await _dataStoreProvider.AddContainer(request.Name, request.PathName, container, ct).ConfigureAwait(false);
        if (addResult.IsFailure)
            return addResult.ToNewResult<DataStoreContainerResponse>();

        DataStoreEndpointLog.ContainerAdded(Logger, request.ContainerName, request.PathName, request.Name);

        var dto = new DataStoreContainerResponse
        {
            Id = container.Id,
            Name = container.Name,
            FieldCount = container.Fields.Count,
            // Why: reflects what the cascade in AddContainer just persisted (Id/DataContainerId are
            // mutated in place on these same objects by SaveOneChild/CascadeCollections) rather than a
            // stale empty list.
            Fields = container.Fields.Select(f => new DataStoreFieldResponse
            {
                Id = f.Id,
                Name = f.Name,
                FrameworkDataType = f.DataType,
                IsNullable = f.IsNullable,
                Ordinal = f.Ordinal,
                Description = f.Description,
            }).ToList(),
            SurrogateKeyFields = [],
            NaturalKeyFields = [],
        };
        // Why: ContainerType is optional on the request; only overwrite when a type was supplied.
        if (container.TypeId is not null)
            dto.ContainerType = container.TypeId;

        return GenericResult<DataStoreContainerResponse>.Success(dto);
    }
}
