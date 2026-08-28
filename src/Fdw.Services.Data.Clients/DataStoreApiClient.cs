namespace Fdw.Services.Data.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.Clients.Abstractions;

/// <summary>
/// API client for DataStore management endpoints.
/// </summary>
public class DataStoreApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public DataStoreApiClient(HttpClient httpClient, ILogger<DataStoreApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets all configured DataStores.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of DataStore summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<DataStoreSummaryPayload>>> GetDataStores(CancellationToken ct = default)
        => GetList<DataStoreSummaryPayload>("datastores", ct);

    /// <summary>
    /// Gets a specific DataStore by name.
    /// </summary>
    /// <param name="name">The DataStore name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the DataStore details.</returns>
    public virtual Task<IGenericResult<DataStoreDetailPayload>> GetDataStore(string name, CancellationToken ct = default)
        => Get<DataStoreDetailPayload>($"datastores/{name}", ct);

    /// <summary>
    /// Creates a new DataStore.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created DataStore details.</returns>
    public virtual Task<IGenericResult<DataStoreDetailPayload>> CreateDataStore(CreateDataStoreWithPathsRequest request, CancellationToken ct = default)
        => Post<CreateDataStoreWithPathsRequest, DataStoreDetailPayload>("datastores", request, ct);

    /// <summary>
    /// Updates an existing DataStore.
    /// </summary>
    /// <param name="name">The DataStore name.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated DataStore details.</returns>
    public virtual Task<IGenericResult<DataStoreDetailPayload>> UpdateDataStore(string name, UpdateDataStoreWithPathsRequest request, CancellationToken ct = default)
        => Patch<UpdateDataStoreWithPathsRequest, DataStoreDetailPayload>($"datastores/{name}", request, ct);

    /// <summary>
    /// Deletes a DataStore.
    /// </summary>
    /// <param name="name">The DataStore name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the deletion succeeded.</returns>
    public virtual Task<IGenericResult> DeleteDataStore(string name, CancellationToken ct = default)
        => Delete($"datastores/{name}", ct);

    /// <summary>
    /// Discovers the schema (paths, containers, fields) for a configured data store and returns
    /// the discovery summary.
    /// </summary>
    /// <param name="request">The discovery request identifying the data store and discovery options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the discovery summary.</returns>
    public virtual Task<IGenericResult<DiscoveryResultPayload>> DiscoverContainers(DiscoverDataStoreRequest request, CancellationToken ct = default)
        => Post<DiscoverDataStoreRequest, DiscoveryResultPayload>("datastores/-/discover", request, ct);

    /// <summary>
    /// Adds a container to an existing data store path.
    /// </summary>
    /// <param name="dataStoreName">The data store name.</param>
    /// <param name="request">The add-container request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created container details.</returns>
    public virtual Task<IGenericResult<DataStoreContainerPayload>> AddDataStoreContainer(
        string dataStoreName,
        AddDataStoreContainerPayload request,
        CancellationToken ct = default)
        => Post<AddDataStoreContainerPayload, DataStoreContainerPayload>(
            $"datastores/{Uri.EscapeDataString(dataStoreName)}/containers", request, ct);

    /// <summary>
    /// Runs the DataStore setup wizard: creates or reuses a connection, tests it,
    /// discovers the full schema, and persists the DataStore hierarchy.
    /// </summary>
    /// <param name="request">The setup request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the setup outcome.</returns>
    public virtual Task<IGenericResult<SetupDataStoreResult>> SetupDataStore(SetupDataStoreRequest request, CancellationToken ct = default)
        => Post<SetupDataStoreRequest, SetupDataStoreResult>("datastores/setup", request, ct);

    /// <summary>
    /// Gets all available DataStore types from the server's DataStoreTypes TypeCollection.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of DataStore type summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<DataStoreTypeSummaryPayload>>> GetTypes(CancellationToken ct = default)
        => GetList<DataStoreTypeSummaryPayload>("datastores/types", ct);
}
