using Fdw.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Components.Logging;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Clients;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Components.DataStores;

/// <summary>
/// The UI-side <see cref="IDomainConfigurationProvider{TImplementationConfiguration}"/> for <see cref="DataStoreImplementationConfiguration"/>
/// — feeds <c>ConfiguredDataStoreProvider</c> (<c>Fdw.Data.DataNodes</c>) from <see cref="DataStoreApiClient"/>
/// instead of a gateway. Mirrors the shallow/full split the server-side
/// <c>DataStoreConfigurationProvider</c>/<c>ConfiguredDataStoreProvider</c> pair already uses:
/// <see cref="Get(CancellationToken)"/> maps the summary endpoint (no <c>Paths</c>), and
/// <see cref="Get(string,CancellationToken)"/> maps the detail endpoint (full <c>Paths → Containers → Fields</c>)
/// — the same two-tier shape <c>ConfiguredDataStoreProvider.Get(CancellationToken)</c> already composes by
/// calling back into <c>Get(name)</c> per shallow header.
/// </summary>
public sealed class ClientsDataStoreConfigurationProvider : IDomainConfigurationProvider<IDataStoreImplementationConfiguration>, IServiceConfigurationProvider
{
    private readonly ILogger<ClientsDataStoreConfigurationProvider> _logger;
    private readonly DataStoreApiClient _apiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientsDataStoreConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger for provider diagnostics.</param>
    /// <param name="apiClient">The DataStore API client (scoped-registered by <c>DataStoreClientType</c>).</param>
    public ClientsDataStoreConfigurationProvider(
        ILogger<ClientsDataStoreConfigurationProvider>? logger,
        DataStoreApiClient apiClient)
    {
        _logger = logger ?? NullLogger<ClientsDataStoreConfigurationProvider>.Instance;
        ArgumentNullException.ThrowIfNull(apiClient);
        _apiClient = apiClient;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataStoreImplementationConfiguration>> Get(string name, CancellationToken ct = default)
    {
        DataStoreProviderLog.TraceGetByNameEntry(_logger, name);

        var dtoResult = await _apiClient.GetDataStore(name, ct).ConfigureAwait(false);
        if (!dtoResult.IsSuccess)
            return dtoResult.ToNewResult<DataStoreImplementationConfiguration>();

        if (dtoResult.Value is null)
            return GenericResult<DataStoreImplementationConfiguration>.Failure(DataStoreProviderLog.ClientReturnedNullStore(_logger, name));

        var configuration = MapDetail(dtoResult.Value);
        DataStoreProviderLog.StoreMapped(_logger, name, configuration.Paths.Count);
        return GenericResult<DataStoreImplementationConfiguration>.Success(configuration);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataStoreImplementationConfiguration>> Get(Guid id, CancellationToken ct = default)
    {
        DataStoreProviderLog.TraceGetByIdEntry(_logger, id);

        var listResult = await _apiClient.GetDataStores(ct).ConfigureAwait(false);
        if (!listResult.IsSuccess)
            return listResult.ToNewResult<DataStoreImplementationConfiguration>();

        var summary = (listResult.Value ?? []).FirstOrDefault(s => s.Id == id);
        if (summary is null)
            return GenericResult<DataStoreImplementationConfiguration>.Failure(DataStoreProviderLog.StoreByIdNotFound(_logger, id));

        return await Get(summary.Name, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IDataStoreImplementationConfiguration>> Get(Guid id, DateTimeOffset asOf, CancellationToken ct = default)
        => Task.FromResult(GenericResult<IDataStoreImplementationConfiguration>.Failure(
            DataStoreProviderLog.AsOfReadNotSupported(_logger, id, asOf)));

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<T>>> Find<T>(
        Func<T, bool> predicate,
        CancellationToken ct = default)
        where T : IDataStoreImplementationConfiguration
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var allResult = await Get(ct).ConfigureAwait(false);
        if (!allResult.IsSuccess || allResult.Value is null)
            return allResult.ToNewResult<IReadOnlyList<T>>();

        return GenericResult<IReadOnlyList<T>>.Success([.. allResult.Value.OfType<T>().Where(predicate)]);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<IDataStoreImplementationConfiguration>>> Get(CancellationToken ct = default)
    {
        DataStoreProviderLog.TraceGetAllEntry(_logger);

        var listResult = await _apiClient.GetDataStores(ct).ConfigureAwait(false);
        if (!listResult.IsSuccess)
            return listResult.ToNewResult<IReadOnlyList<DataStoreImplementationConfiguration>>();

        var configurations = (listResult.Value ?? []).Select(MapSummary).ToList();
        DataStoreProviderLog.AllStoresMapped(_logger, configurations.Count);
        return GenericResult<IReadOnlyList<DataStoreImplementationConfiguration>>.Success(configurations);
    }

    /// <inheritdoc/>
    public Task<IGenericResult> Save<T>(
        T implementationConfiguration,
        string domain,
        string implementationName,
        string name,
        CancellationToken ct = default)
        where T : IDataStoreImplementationConfiguration
        => Task.FromResult(GenericResult.Failure(DataStoreProviderLog.SaveNotSupported(_logger, name)));

    /// <inheritdoc/>
    public IGenericResult Register<T>(string name, T implementationConfigurationProvider)
        => GenericResult.Failure(DataStoreProviderLog.RegisterNotSupported(_logger, name));

    /// <inheritdoc/>
    public Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
        => Task.FromResult(GenericResult.Failure(DataStoreProviderLog.DeleteByIdNotSupported(_logger, id)));

    /// <inheritdoc/>
    public Task<IGenericResult> Delete(string name, CancellationToken ct = default)
        => Task.FromResult(GenericResult.Failure(DataStoreProviderLog.DeleteByNameNotSupported(_logger, name)));


    private static DataStoreImplementationConfiguration MapDetail(DataStoreDetailPayload dto)
    {
        var configuration = new DataStoreImplementationConfiguration
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            Implementation = dto.StoreType,
            IsActive = dto.IsActive,
            WriteMode = dto.WriteMode,
            LastDiscoveredAt = dto.LastDiscoveredAt,
            CreateDate = dto.CreatedAt,
            CreateBy = dto.CreatedBy,
            CreateOnBehalfOf = dto.CreatedOnBehalfOf,
            ModifyBy = dto.ModifiedBy,
            ModifyOnBehalfOf = dto.ModifiedOnBehalfOf,
        };
        if (dto.ModifiedAt.HasValue)
            configuration.ModifyDate = dto.ModifiedAt.Value;

        configuration.Paths = dto.Paths.Select(pathDto => MapPath(pathDto, configuration.Id)).ToList();
        return configuration;
    }

    private static DataStoreImplementationConfiguration MapSummary(DataStoreSummaryPayload dto)
    {
        var configuration = new DataStoreImplementationConfiguration
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            LastDiscoveredAt = dto.LastDiscoveredAt,
            CreateDate = dto.CreatedAt,
            CreateBy = dto.CreatedBy,
            CreateOnBehalfOf = dto.CreatedOnBehalfOf,
            ModifyBy = dto.ModifiedBy,
            ModifyOnBehalfOf = dto.ModifiedOnBehalfOf,
        };
        if (dto.ModifiedAt.HasValue)
            configuration.ModifyDate = dto.ModifiedAt.Value;

        return configuration;
    }

    private static DataPathConfiguration MapPath(DataStorePathPayload dto, Guid dataStoreId)
    {
        var configuration = new DataPathConfiguration
        {
            Id = dto.Id,
            Name = dto.Name,
            PathValue = dto.PhysicalPath,
            Description = dto.Description,
            SourceDescription = dto.SourceDescription,
            PathType = dto.PathType,
            DataStoreImplementationId = dataStoreId,
        };

        configuration.Containers = dto.Containers.Select(containerDto => MapContainer(containerDto, configuration.Id)).ToList();
        return configuration;
    }

    private static DataContainerConfiguration MapContainer(DataStoreContainerPayload dto, Guid dataPathId)
    {
        var configuration = new DataContainerConfiguration
        {
            Id = dto.Id,
            Name = dto.Name,
            TypeId = dto.ContainerType,
            DataPathId = dataPathId,
        };

        configuration.Fields = dto.Fields.Select(fieldDto => MapField(fieldDto, configuration.Id)).ToList();
        return configuration;
    }

    private static DataContainerFieldConfiguration MapField(DataStoreFieldPayload dto, Guid dataContainerId)
    {
        return new DataContainerFieldConfiguration
        {
            Id = dto.Id,
            Name = dto.Name,
            DataType = dto.NativeDataType,
            IsNullable = dto.IsNullable,
            Ordinal = dto.Ordinal,
            DataContainerId = dataContainerId,
        };
    }

    // ── Type-erased surface ─────────────────────────────────────────────────

    async Task<IGenericResult<IGenericConfiguration>> IServiceConfigurationProvider.Get(Guid id, CancellationToken ct)
    {
        var result = await Get(id, ct).ConfigureAwait(false);
        return result.IsSuccess
            ? result.ToNewResult<IGenericConfiguration>(result.Value!)
            : result.ToNewResult<IGenericConfiguration>();
    }

    async Task<IGenericResult<IGenericConfiguration>> IServiceConfigurationProvider.Get(string name, CancellationToken ct)
    {
        var result = await Get(name, ct).ConfigureAwait(false);
        return result.IsSuccess
            ? result.ToNewResult<IGenericConfiguration>(result.Value!)
            : result.ToNewResult<IGenericConfiguration>();
    }

    Task<IGenericResult> IServiceConfigurationProvider.Save(IGenericConfiguration record, CancellationToken ct)
        => record is DataStoreImplementationConfiguration typed
            ? Task.FromResult(GenericResult.Failure(DataStoreProviderLog.SaveNotSupported(_logger, typed.Name)))
            : Task.FromResult(GenericResult.Failure(
                DataStoreProviderLog.UntypedSaveTypeMismatch(
                    _logger, nameof(DataStoreImplementationConfiguration), record?.GetType().Name ?? "null")));

}
