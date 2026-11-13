using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Data.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for setting the paging a container of a data store states for itself, so a container that pages differently from its connection is read in
/// its own style (FDW-5562).
/// PATCH datastores/{Name}/containers/{ContainerId}/paging
/// </summary>
/// <remarks>
/// The request replaces the container's own paging: the style it names and the settings it states. The container's connection states the paging of every container
/// that states none, and a container returns to it with the delete of its own paging. What a style reads is the store type's to say, so nothing here names a
/// connection type.
/// </remarks>
public abstract class SetDataStoreContainerPagingEndpointBase : CrudUpdateEndpointBase<SetDataStoreContainerPagingRequest, DataStoreContainerPagingDto>
{
    private readonly IDataStoreConfigurationProvider _dataStoreProvider;
    private readonly IConnectionConfigurationProvider _connectionProvider;

    /// <summary>Initializes a new instance of the <see cref="SetDataStoreContainerPagingEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads and saves the data store the container is in.</param>
    /// <param name="connectionProvider">Reads the connection of the data store, which the container's own paging is checked against.</param>
    protected SetDataStoreContainerPagingEndpointBase(
        ILogger<SetDataStoreContainerPagingEndpointBase> logger, IDataStoreConfigurationProvider dataStoreProvider, IConnectionConfigurationProvider connectionProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
        _connectionProvider = connectionProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Gets the nested-resource route.</summary>
    protected override string Route => "/datastores/{Name}/containers/{ContainerId}/paging";

    /// <summary>Gets the summary shown for the route.</summary>
    protected override string EndpointSummary => "Set the paging a container states for itself";

    /// <summary>Gets the description shown for the route.</summary>
    protected override string EndpointDescription =>
        "Names the paging style a container of an HTTP data store pages in and the settings it states for it; what it does not state is taken from its connection when it pages in the connection's style, and from the style's declared default otherwise.";

    /// <summary>Returns the data store and container as the resource identifier.</summary>
    protected override string GetResourceIdentifier(SetDataStoreContainerPagingRequest request) => $"{request.Name}/{request.ContainerId}";

    /// <summary>Finds the container and reports its paging; one that does not exist answers 404.</summary>
    protected override async Task<IGenericResult<DataStoreContainerPagingDto?>> FindForUpdate(SetDataStoreContainerPagingRequest request, CancellationToken ct)
    {
        var found = await ContainerPagingBuilder.Locate(_dataStoreProvider, request.Name, request.ContainerId, ct).ConfigureAwait(false);
        if (found.IsFailure)
            return found.ToNewResult<DataStoreContainerPagingDto?>();

        if (found.Value is null)
            return GenericResult<DataStoreContainerPagingDto?>.Success(null);

        // The caller's tenant is asked before anything is said about the store: a refusal that names the store's type or its connection would tell a caller what a store
        // it may not change is made of (FDW-5562).
        var permitted = await _dataStoreProvider.PermitWrite(found.Value.Store.Id, ct).ConfigureAwait(false);
        if (permitted.IsFailure)
            return permitted.ToNewResult<DataStoreContainerPagingDto?>();

        var report = await ContainerPagingBuilder.Report(_dataStoreProvider, _connectionProvider, found.Value, Logger, ct).ConfigureAwait(false);
        return report.IsFailure
            ? report.ToNewResult<DataStoreContainerPagingDto?>()
            : GenericResult<DataStoreContainerPagingDto?>.Success(report.Value);
    }

    /// <summary>Checks the paging the request states against the container's connection, gives it to the container and saves the store.</summary>
    protected override async Task<IGenericResult<DataStoreContainerPagingDto>> Update(
        SetDataStoreContainerPagingRequest request, DataStoreContainerPagingDto existing, CancellationToken ct)
    {
        var found = await ContainerPagingBuilder.Locate(_dataStoreProvider, request.Name, request.ContainerId, ct).ConfigureAwait(false);
        if (found.IsFailure || found.Value is not { } location)
            return GenericResult<DataStoreContainerPagingDto>.Failure(DataStoreEndpointLog.DataStoreNotFound(Logger, request.Name));

        var style = ContainerPagingBuilder.StyleOf(request.PagingStyle);
        if (style is null)
        {
            return GenericResult<DataStoreContainerPagingDto>.Failure(
                DataServiceResultCodes.ByName("ContainerPagingNothingStated"), Logger,
                ResultDetails.Create("container", location.Container.Name));
        }

        var connection = await _connectionProvider.Get(location.Store.ConnectionId, ct).ConfigureAwait(false);
        if (connection.IsFailure || connection.Value is null)
            return GenericResult<DataStoreContainerPagingDto>.Failure(DataStoreEndpointLog.ConnectionNameUnresolved(Logger, location.Store.ConnectionId, location.Store.Name));

        var settings = ContainerPagingBuilder.Stated(request.PagingSettings);
        var refusal = ContainerPagingBuilder.Refusal(
            DataStoreTypes.ByName(location.Store.Implementation), connection.Value, location.Container.Name, style, settings, Logger);
        if (refusal is not null)
            return refusal.ToNewResult<DataStoreContainerPagingDto>();

        ContainerPagingBuilder.Apply(location.Container, style, settings);
        var saved = await _dataStoreProvider.UpdateStore(location.Store, ct).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<DataStoreContainerPagingDto>()
            : await ContainerPagingBuilder.Report(_dataStoreProvider, _connectionProvider, location, Logger, ct).ConfigureAwait(false);
    }
}
