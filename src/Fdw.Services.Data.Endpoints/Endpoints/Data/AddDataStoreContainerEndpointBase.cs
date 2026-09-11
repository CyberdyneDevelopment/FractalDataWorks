using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for adding a container to an existing data store path.
/// POST datastores/{name}/containers
/// </summary>
public abstract class AddDataStoreContainerEndpointBase : CrudCreateEndpointBase<AddDataStoreContainerRequest, DataStoreContainerResponse>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected AddDataStoreContainerEndpointBase(ILogger<AddDataStoreContainerEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider) : base(logger)
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
            Format = request.Format,
            RecordSelector = request.RecordSelector,
            FlattenNestedObjects = request.FlattenNestedObjects,
            FlattenSeparator = request.FlattenSeparator,
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

        // A container hangs from a path, which hangs from the store, so the write is a save of the
        // store carrying it. The store-exists and path-exists invariants the old AddContainer
        // enforced are stated here, where the rows they are about are in hand.
        var storeResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
            return GenericResult<DataStoreContainerResponse>.Failure(
                DataStoreEndpointLog.DataStoreNotFound(Logger, request.Name));

        var store = storeResult.Value;
        var path = store.Paths.FirstOrDefault(
            p => string.Equals(p.Name, request.PathName, StringComparison.OrdinalIgnoreCase));
        if (path is null)
            return GenericResult<DataStoreContainerResponse>.Failure(
                DataStoreEndpointLog.PathNotFoundInDataStore(Logger, request.PathName, request.Name));

        if (path.Containers.Any(c => string.Equals(c.Name, container.Name, StringComparison.OrdinalIgnoreCase)))
            return GenericResult<DataStoreContainerResponse>.Failure(
                DataStoreEndpointLog.ContainerAlreadyExists(Logger, container.Name, request.PathName, request.Name));

        path.Containers.Add(container);

        var addResult = await _dataStoreProvider
            .Save(store, store.Domain, store.Implementation, store.Name, ct)
            .ConfigureAwait(false);
        if (addResult.IsFailure)
            return addResult.ToNewResult<DataStoreContainerResponse>();

        DataStoreEndpointLog.ContainerAdded(Logger, request.ContainerName, request.PathName, request.Name);

        var dto = new DataStoreContainerResponse
        {
            Id = container.Id,
            Name = container.Name,
            FieldCount = container.Fields.Count,
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
        if (container.TypeId is not null)
            dto.ContainerType = container.TypeId;

        return GenericResult<DataStoreContainerResponse>.Success(dto);
    }
}
