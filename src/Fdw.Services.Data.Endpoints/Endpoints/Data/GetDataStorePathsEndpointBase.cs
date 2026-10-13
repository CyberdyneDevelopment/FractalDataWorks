using System.Collections.Generic;
using System;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
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
/// Base endpoint for retrieving paths for a specific data store.
/// </summary>
public abstract class GetDataStorePathsEndpointBase : CrudGetEndpointBase<DataStoreNameRequest, List<DataStorePathResponse>>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected GetDataStorePathsEndpointBase(ILogger<GetDataStorePathsEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Returns the data store name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataStoreNameRequest request) => request.Name;

    /// <summary>Finds a data store by name and returns its paths, each with its containers.</summary>
    /// <remarks>
    /// Reads the store's paths and their containers as records only, not the store's other collections
    /// and not the containers' fields: a container's fields are read when it is opened (FDW-951). A
    /// store that does not exist fails as not found.
    /// </remarks>
    protected override async Task<IGenericResult<List<DataStorePathResponse>?>> FindByIdentifier(DataStoreNameRequest request, CancellationToken ct)
    {
        var containers = QueryFlag.Read(HttpContext, "containers", Logger, whenAbsent: true);
        if (containers.IsFailure)
            return containers.ToNewResult<List<DataStorePathResponse>?>();

        // Without containers the paths are read as records only: the containers are not read to be left out (FDW-1194).
        var level = containers.Value ? ConfigurationLevels.Children : ConfigurationLevels.RecordOnly;
        var paths = await _dataStoreProvider.Get<DataPathConfiguration>(request.Name, level, ct).ConfigureAwait(false);
        return paths.IsFailure
            ? paths.ToNewResult<List<DataStorePathResponse>?>()
            : GenericResult<List<DataStorePathResponse>?>.Success(paths.Value!.Select(MapPath).ToList());
    }

    /// <summary>Maps a path configuration to a path DTO.</summary>
    protected virtual DataStorePathResponse MapPath(DataPathConfiguration path)
    {
        return new DataStorePathResponse
        {
            Id = path.Id,
            Name = path.Name,
            PathType = path.PathType ?? string.Empty,
            PathValue = path.PathValue,
            Description = path.Description,
            ClassificationId = path.ClassificationId,
            Containers = path.Containers.Select(MapContainer).ToList()
        };
    }

    /// <summary>Maps a container configuration to a container summary DTO.</summary>
    protected virtual DataStoreContainerResponse MapContainer(DataContainerConfiguration container)
    {
        return new DataStoreContainerResponse
        {
            Id = container.Id,
            Name = container.Name,
            ContainerType = container.TypeId ?? string.Empty,
            ClassificationId = container.ClassificationId,
            Format = container.Format,
            HasHeader = container.HasHeader,
            Separator = container.Separator,
            SheetName = container.SheetName
        };
    }

    /// <summary>
    /// Sends the paths with their containers, or without them when the caller asks with <c>?containers=false</c> (FDW-1194).
    /// </summary>
    /// <param name="found">The store's paths.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the send.</returns>
    /// <remarks>
    /// The paths are sent as <see cref="DataStorePathSummaryResponse"/>, which has no containers member, so a path is never
    /// described as having none because they were not asked for. Absent, <c>containers</c> is true and the paths are sent as
    /// they always were.
    /// </remarks>
    protected override async Task SendFound(List<DataStorePathResponse> found, CancellationToken ct)
    {
        var containers = QueryFlag.Read(HttpContext, "containers", Logger, whenAbsent: true);
        if (containers.IsFailure)
        {
            await SendErrorResponse(containers, ct).ConfigureAwait(false);
            return;
        }

        if (containers.Value)
        {
            await base.SendFound(found, ct).ConfigureAwait(false);
            return;
        }

        var paths = found.Select(path => new DataStorePathSummaryResponse
        {
            Id = path.Id,
            Name = path.Name,
            PathType = path.PathType,
            PathValue = path.PathValue,
            Description = path.Description,
            ClassificationId = path.ClassificationId,
        }).ToList();

        await Send.ResultAsync(TypedResults.Json(paths)).ConfigureAwait(false);
    }
}
