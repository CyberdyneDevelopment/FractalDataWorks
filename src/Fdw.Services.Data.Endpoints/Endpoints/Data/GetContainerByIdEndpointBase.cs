using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for retrieving a specific container by its identifier.
/// </summary>
/// <remarks>
/// With only the container's id there is no store to start from, so every store is read down to its
/// containers to find it, and then only that container's fields (FDW-951). No other container's
/// fields are read.
/// </remarks>
public abstract class GetContainerByIdEndpointBase : CrudGetEndpointBase<ContainerIdRequest, DataStoreContainerDetailDto>
{
    // A store's paths are one level under it and their containers two.
    private const int ThroughContainers = 2;

    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected GetContainerByIdEndpointBase(ILogger<GetContainerByIdEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastore-containers";

    /// <summary>Returns the container identifier.</summary>
    protected override string GetResourceIdentifier(ContainerIdRequest request) => request.Id.ToString();

    /// <summary>Finds a container by its identifier, with its fields; one that does not exist answers 404.</summary>
    protected override async Task<IGenericResult<DataStoreContainerDetailDto?>> FindByIdentifier(ContainerIdRequest request, CancellationToken ct)
    {
        var stores = await _dataStoreProvider.Get(ThroughContainers, ct).ConfigureAwait(false);
        if (stores.IsFailure)
        {
            return stores.ToNewResult<DataStoreContainerDetailDto?>();
        }

        var found = stores.Value!
            .SelectMany(store => store.Paths.SelectMany(path => path.Containers.Select(container => (store, container))))
            .FirstOrDefault(pair => pair.container.Id == request.Id);
        if (found.container is null)
        {
            return GenericResult<DataStoreContainerDetailDto?>.Success(null);
        }

        var fields = await ContainerFields.Get(_dataStoreProvider, found.store.Name, found.container.Id, ct).ConfigureAwait(false);
        return fields.IsFailure
            ? fields.ToNewResult<DataStoreContainerDetailDto?>()
            : GenericResult<DataStoreContainerDetailDto?>.Success(MapToDetail(found.container, fields.Value!));
    }

    /// <summary>Maps a container configuration and its fields to a detail DTO.</summary>
    /// <param name="container">The container, read as a record.</param>
    /// <param name="fields">The container's fields.</param>
    /// <returns>The detail DTO.</returns>
    protected virtual DataStoreContainerDetailDto MapToDetail(DataContainerConfiguration container, IList<DataStoreFieldResponse> fields)
    {
        return new DataStoreContainerDetailDto
        {
            Id = container.Id,
            Name = container.Name,
            ContainerType = container.TypeId ?? string.Empty,
            Description = container.Description,
            ClassificationId = container.ClassificationId,
            Format = container.Format,
            HasHeader = container.HasHeader,
            Separator = container.Separator,
            SheetName = container.SheetName,
            Fields = fields
        };
    }
}
