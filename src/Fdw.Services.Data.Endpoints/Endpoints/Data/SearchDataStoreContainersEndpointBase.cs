using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Logging;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for finding the containers of one data store across all of its paths, one page at a time (FDW-1194).
/// </summary>
/// <remarks>
/// <c>GET /datastores/{Name}/containers?q=&amp;kind=&amp;pathId=&amp;skip=&amp;take=&amp;total=true</c>. The store's paths and their
/// containers are read once, as records only (their fields are read when a container is opened, FDW-951), and the search
/// and the page are cut in memory from that read, so one request replaces a request per path. <c>q</c> is compared with a
/// container's name, the name of its path and its kind, in the way <c>match</c> says; <c>kind</c> keeps one kind of
/// container (without regard to case); <c>pathId</c> keeps one path's. Items come in path-name, then container-name order.
/// <para>
/// A store that does not exist fails as not found, and so does a <c>pathId</c> the store has no path for: a path that was
/// asked for and is not there is never answered as a path with no containers.
/// </para>
/// </remarks>
public abstract class SearchDataStoreContainersEndpointBase : CrudPageEndpointBase<DataStoreContainerListItem, DataStoreContainerListItem>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <summary>Initializes a new instance of the <see cref="SearchDataStoreContainersEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads the store's paths and their containers.</param>
    protected SearchDataStoreContainersEndpointBase(
        ILogger<SearchDataStoreContainersEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider)
        : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/containers";

    /// <inheritdoc />
    protected override string EndpointSummary => "Find the containers of a data store";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Streams one page of the store's containers across its paths, in path-name then container-name order, found by ?q= " +
        "(compared with the container name, its path name and its kind as ?match= says), ?kind= and ?pathId=. " +
        "?total=true adds how many containers were selected in all.";

    /// <inheritdoc />
    protected override bool Searchable => true;

    /// <summary>Gets whether the path is named in the route (<c>{PathId}</c>) and not in the query (<c>?pathId=</c>).</summary>
    protected virtual bool PathInRoute => false;

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConfigurationPage<DataStoreContainerListItem>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        var storeName = HttpContext.Request.RouteValues.TryGetValue("Name", out var named) ? named?.ToString() : null;
        if (string.IsNullOrWhiteSpace(storeName))
            return GenericResult<ConfigurationPage<DataStoreContainerListItem>>.Failure(
                EndpointLogger.PaginationValueInvalid(Logger, "Name", string.Empty, "the name of a data store"));

        var pathId = PathFilter();
        if (pathId.IsFailure)
            return pathId.ToNewResult<ConfigurationPage<DataStoreContainerListItem>>();

        var paths = await _dataStoreProvider.Get<DataPathConfiguration>(storeName, ConfigurationLevels.Children, ct).ConfigureAwait(false);
        if (paths.IsFailure)
            return paths.ToNewResult<ConfigurationPage<DataStoreContainerListItem>>();

        if (pathId.Value is { } wanted && paths.Value!.All(path => path.Id != wanted))
            return GenericResult<ConfigurationPage<DataStoreContainerListItem>>.Failure(
                DataStoreEndpointLog.PathNotFoundInDataStore(Logger, wanted.ToString(), storeName));

        var selected = Selected(paths.Value!, pathId.Value);

        var page = selected.Skip(paging.Skip).Take(paging.Take).ToList();
        return GenericResult<ConfigurationPage<DataStoreContainerListItem>>.Success(new ConfigurationPage<DataStoreContainerListItem>(
            paging.Skip,
            paging.Take,
            selected.Count > paging.Skip + paging.Take,
            _ => page.ToAsyncEnumerable(),
            Search.IncludeTotal ? selected.Count : null));
    }

    // The containers the request keeps, in path-name then container-name order: those of the path it names, of the kind it
    // names, that the search finds by container name, path name or kind.
    private List<DataStoreContainerListItem> Selected(IEnumerable<DataPathConfiguration> paths, Guid? pathId)
    {
        var kind = HttpContext.Request.Query.TryGetValue("kind", out var asked) && !string.IsNullOrWhiteSpace(asked.ToString())
            ? asked.ToString()
            : null;

        return paths
            .Where(path => pathId is null || path.Id == pathId)
            .OrderBy(path => path.Name, StringComparer.OrdinalIgnoreCase)
            .SelectMany(path => path.Containers
                .OrderBy(container => container.Name, StringComparer.OrdinalIgnoreCase)
                .Select(container => new DataStoreContainerListItem
                {
                    Id = container.Id,
                    Name = container.Name,
                    ContainerType = container.TypeId,
                    ClassificationId = container.ClassificationId,
                    PathId = path.Id,
                    PathName = path.Name,
                }))
            .Where(item => (kind is null || string.Equals(item.ContainerType, kind, StringComparison.OrdinalIgnoreCase))
                && Search.Selects(item.Name, item.PathName, item.ContainerType))
            .ToList();
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<DataStoreContainerListItem>> ToSummary(DataStoreContainerListItem item, CancellationToken ct)
        => Task.FromResult(GenericResult<DataStoreContainerListItem>.Success(item));

    // The path the request keeps, from the route or the query; nothing when the request keeps them all.
    private IGenericResult<Guid?> PathFilter()
    {
        string? text;
        if (PathInRoute)
            text = HttpContext.Request.RouteValues.TryGetValue("PathId", out var routed) ? routed?.ToString() : null;
        else
            text = HttpContext.Request.Query.TryGetValue("pathId", out var asked) ? asked.ToString() : null;

        if (string.IsNullOrWhiteSpace(text))
            return PathInRoute
                ? GenericResult<Guid?>.Failure(EndpointLogger.PaginationValueInvalid(Logger, "PathId", string.Empty, "the identifier of a path"))
                : GenericResult<Guid?>.Success(null);

        return Guid.TryParse(text, out var id)
            ? GenericResult<Guid?>.Success(id)
            : GenericResult<Guid?>.Failure(EndpointLogger.PaginationValueInvalid(Logger, PathInRoute ? "PathId" : "pathId", text, "the identifier of a path"));
    }
}
