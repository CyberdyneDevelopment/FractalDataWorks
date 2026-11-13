using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for returning a container of a data store to its connection's paging: the paging the container states for itself is removed (FDW-5562).
/// DELETE datastores/{Name}/containers/{ContainerId}/paging
/// </summary>
/// <remarks>
/// Deletes only the container's own paging, never the container. A container that states none is returned as it is: it already takes its connection's.
/// </remarks>
public abstract class ClearDataStoreContainerPagingEndpointBase : CrudDeleteEndpointBase<DataStoreContainerPagingRequest>
{
    private readonly IDataStoreConfigurationProvider _dataStoreProvider;

    /// <summary>Initializes a new instance of the <see cref="ClearDataStoreContainerPagingEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads and saves the data store the container is in.</param>
    protected ClearDataStoreContainerPagingEndpointBase(ILogger<ClearDataStoreContainerPagingEndpointBase> logger, IDataStoreConfigurationProvider dataStoreProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Gets the write policy: removing a container's paging changes the data store, as adding a container does.</summary>
    protected override string DeletePolicy => "datastores:write";

    /// <summary>Gets the nested-resource route.</summary>
    protected override string Route => "/datastores/{Name}/containers/{ContainerId}/paging";

    /// <summary>Gets the summary shown for the route.</summary>
    protected override string EndpointSummary => "Return a container to its connection's paging";

    /// <summary>Gets the description shown for the route.</summary>
    protected override string EndpointDescription => "Removes the paging a container states for itself, so it is read in its connection's paging style and settings.";

    /// <summary>Returns the data store and container as the resource identifier.</summary>
    protected override string GetResourceIdentifier(DataStoreContainerPagingRequest request) => $"{request.Name}/{request.ContainerId}";

    /// <summary>Checks that the container exists.</summary>
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(DataStoreContainerPagingRequest request, CancellationToken ct)
    {
        var found = await ContainerPagingBuilder.Locate(_dataStoreProvider, request.Name, request.ContainerId, ct).ConfigureAwait(false);
        return found.IsFailure ? found.ToNewResult<bool>() : GenericResult<bool>.Success(found.Value is not null);
    }

    /// <summary>Removes the container's own paging and saves the data store.</summary>
    protected override async Task<IGenericResult> Delete(DataStoreContainerPagingRequest request, CancellationToken ct)
    {
        var found = await ContainerPagingBuilder.Locate(_dataStoreProvider, request.Name, request.ContainerId, ct).ConfigureAwait(false);
        if (found.IsFailure || found.Value is not { } location)
            return found;

        ContainerPagingBuilder.Apply(location.Container, null, ImmutableDictionary<string, string?>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase));
        return await _dataStoreProvider.UpdateStore(location.Store, ct).ConfigureAwait(false);
    }
}
