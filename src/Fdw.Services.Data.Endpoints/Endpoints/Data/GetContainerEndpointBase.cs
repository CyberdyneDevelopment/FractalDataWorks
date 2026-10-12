using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for retrieving a specific container by data store, path, and container name.
/// </summary>
public abstract class GetContainerEndpointBase : CrudGetEndpointBase<GetContainerRequest, DataStoreContainerDetailDto>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected GetContainerEndpointBase(ILogger<GetContainerEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastore-containers";

    /// <summary>Returns the container identifier as a composite key.</summary>
    protected override string GetResourceIdentifier(GetContainerRequest request) =>
        $"{request.Name}/{request.PathName}/{request.ContainerName}";

    /// <summary>Finds a container by data store, path, and container name, with its fields.</summary>
    /// <remarks>
    /// Reads down one branch of the store: its paths as records, the named path's containers as
    /// records, then the named container's fields. The store's other paths and containers are never
    /// composed (FDW-951). A store that does not exist fails as not found; a path or container that
    /// does not exist answers 404.
    /// </remarks>
    protected override async Task<IGenericResult<DataStoreContainerDetailDto?>> FindByIdentifier(GetContainerRequest request, CancellationToken ct)
    {
        var paths = await _dataStoreProvider.Get<DataPathConfiguration>(request.Name, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (paths.IsFailure) return paths.ToNewResult<DataStoreContainerDetailDto?>();

        var path = paths.Value!.FirstOrDefault(p => string.Equals(p.Name, request.PathName, StringComparison.OrdinalIgnoreCase));
        if (path is null)
            return GenericResult<DataStoreContainerDetailDto?>.Success(null);

        var containers = await _dataStoreProvider.Get<DataPathConfiguration, DataContainerConfiguration>(
            request.Name, path.Id, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (containers.IsFailure) return containers.ToNewResult<DataStoreContainerDetailDto?>();

        var container = containers.Value!.FirstOrDefault(c => string.Equals(c.Name, request.ContainerName, StringComparison.OrdinalIgnoreCase));
        if (container is null)
            return GenericResult<DataStoreContainerDetailDto?>.Success(null);

        var fields = await ContainerFields.Get(_dataStoreProvider, request.Name, container.Id, ct).ConfigureAwait(false);
        return fields.IsFailure
            ? fields.ToNewResult<DataStoreContainerDetailDto?>()
            : GenericResult<DataStoreContainerDetailDto?>.Success(MapToDetail(container, fields.Value!));
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
            Fields = fields
        };
    }
}
