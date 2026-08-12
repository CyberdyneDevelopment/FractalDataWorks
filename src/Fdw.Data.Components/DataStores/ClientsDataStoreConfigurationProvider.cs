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
/// The UI-side <see cref="IServiceConfigurationProvider{TConfig}"/> for <see cref="DataStoreConfiguration"/>
/// — feeds <c>ConfiguredDataStoreProvider</c> (<c>Fdw.Data.DataNodes</c>) from <see cref="DataStoreApiClient"/>
/// instead of a gateway. Mirrors the shallow/full split the server-side
/// <c>DataStoreConfigurationProvider</c>/<c>ConfiguredDataStoreProvider</c> pair already uses:
/// <see cref="Get(CancellationToken)"/> maps the summary endpoint (no <c>Paths</c>), and
/// <see cref="Get(string,CancellationToken)"/> maps the detail endpoint (full <c>Paths → Containers → Fields</c>)
/// — the same two-tier shape <c>ConfiguredDataStoreProvider.Get(CancellationToken)</c> already composes by
/// calling back into <c>Get(name)</c> per shallow header.
/// </summary>
public sealed class ClientsDataStoreConfigurationProvider : IServiceConfigurationProvider<DataStoreConfiguration>
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
        // Why: NullLogger<T>.Instance is the only sanctioned ?? fallback (functional without DI logging).
        _logger = logger ?? NullLogger<ClientsDataStoreConfigurationProvider>.Instance;
        ArgumentNullException.ThrowIfNull(apiClient);
        _apiClient = apiClient;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<DataStoreConfiguration>> Get(string name, CancellationToken ct = default)
    {
        DataStoreProviderLog.TraceGetByNameEntry(_logger, name);

        var dtoResult = await _apiClient.GetDataStore(name, ct).ConfigureAwait(false);
        // Why: DataStoreApiClient (ApiClientBase.Get<T>) already MessageLogging-instruments HTTP/deserialization
        // failures — propagate rather than re-log (no double-logging), mirroring ConfiguredDataStoreProvider.
        if (!dtoResult.IsSuccess)
            return dtoResult.ToNewResult<DataStoreConfiguration>();

        if (dtoResult.Value is null)
            return GenericResult<DataStoreConfiguration>.Failure(DataStoreProviderLog.ClientReturnedNullStore(_logger, name));

        var configuration = MapDetail(dtoResult.Value);
        DataStoreProviderLog.StoreMapped(_logger, name, configuration.Paths.Count);
        return GenericResult<DataStoreConfiguration>.Success(configuration);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<DataStoreConfiguration>> Get(Guid id, CancellationToken ct = default)
    {
        DataStoreProviderLog.TraceGetByIdEntry(_logger, id);

        // Why: DataStoreApiClient exposes no get-by-id endpoint (only the list and the by-name detail
        // routes) — resolve the name from the summary list, then delegate to Get(name) for the full
        // detail, mirroring ConfiguredDataStoreProvider.Get(Guid)'s id->name->Get(name) delegation.
        var listResult = await _apiClient.GetDataStores(ct).ConfigureAwait(false);
        if (!listResult.IsSuccess)
            return listResult.ToNewResult<DataStoreConfiguration>();

        var summary = (listResult.Value ?? []).FirstOrDefault(s => s.Id == id);
        if (summary is null)
            return GenericResult<DataStoreConfiguration>.Failure(DataStoreProviderLog.StoreByIdNotFound(_logger, id));

        return await Get(summary.Name, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<DataStoreConfiguration>>> Get(CancellationToken ct = default)
    {
        DataStoreProviderLog.TraceGetAllEntry(_logger);

        var listResult = await _apiClient.GetDataStores(ct).ConfigureAwait(false);
        if (!listResult.IsSuccess)
            return listResult.ToNewResult<IReadOnlyList<DataStoreConfiguration>>();

        var configurations = (listResult.Value ?? []).Select(MapSummary).ToList();
        DataStoreProviderLog.AllStoresMapped(_logger, configurations.Count);
        return GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success(configurations);
    }

    /// <inheritdoc/>
    // Why: this provider composes the READ-ONLY display tree ConfiguredDataStoreProvider builds for
    // UI navigation. DataStore mutations already have a domain write path — DataStoreApiClient's
    // CreateDataStore/UpdateDataStore/DeleteDataStore, invoked directly by DataStoreContext — so Save
    // fails loud rather than silently no-op'ing or duplicating that path.
    public Task<IGenericResult<DataStoreConfiguration>> Save(DataStoreConfiguration record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        return Task.FromResult(GenericResult<DataStoreConfiguration>.Failure(
            DataStoreProviderLog.SaveNotSupported(_logger, record.Name)));
    }

    /// <inheritdoc/>
    public Task<IGenericResult> Delete(Guid id, CancellationToken ct = default)
        => Task.FromResult(GenericResult.Failure(DataStoreProviderLog.DeleteByIdNotSupported(_logger, id)));

    /// <inheritdoc/>
    public Task<IGenericResult> Delete(string name, CancellationToken ct = default)
        => Task.FromResult(GenericResult.Failure(DataStoreProviderLog.DeleteByNameNotSupported(_logger, name)));

    // Why (NO FALLBACKS): fields the DTOs do not carry are left at the configuration POCOs' own
    // defaults, never invented. Known gaps in the display DTOs (DataStoreDetailPayload/DataStorePathPayload/
    // DataStoreContainerPayload/DataStoreFieldPayload) vs the full configuration model:
    //  - DataStoreConfiguration.ConnectionId — the DTO carries ConnectionName (display) only, no
    //    connection Guid; ConnectionId stays Guid.Empty.
    //  - DataContainerConfiguration.Keys / SurrogateKeyFields / NaturalKeyFields — no FK/key graph is
    //    exposed by the display DTO; Keys stays empty ([NotMapped], resolved by DataStoreBuilderBase's
    //    FK-direct pass only when present).
    //  - DataContainerConfiguration.Format / RecordSelector / FlattenNestedObjects / FlattenSeparator —
    //    no format/row-shaping discriminator is exposed per-container; these stay null/unset, so
    //    ContainerComposition.ResolveFormat falls through to GenericBuilderSelector's default format.
    //  - DataStoreContainerPayload.PhysicalName / SourceDescription and DataStoreFieldPayload.IsKey/MaxLength/
    //    Precision/Scale have no destination property on DataContainerConfiguration/
    //    DataContainerFieldConfiguration (those live on typed-body records like MsSqlDataContainerField,
    //    not surfaced by this display DTO) — left unmapped.
    // These are display-tree limitations of the current API surface, not oversights; gaps filed.

    private static DataStoreConfiguration MapDetail(DataStoreDetailPayload dto)
    {
        var configuration = new DataStoreConfiguration
        {
            Id = dto.Id,
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            ServiceOptionType = dto.StoreType,
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

    private static DataStoreConfiguration MapSummary(DataStoreSummaryPayload dto)
    {
        var configuration = new DataStoreConfiguration
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

        // Why: shallow header only — Paths stays at its [] POCO default. ConfiguredDataStoreProvider.Get()
        // composes the full aggregate per store via Get(name), which maps the detail endpoint's Paths.
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
            DataStoreId = dataStoreId,
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
    // Why explicit: only a parent provider calls these, and it holds this provider as the non-generic
    // IServiceConfigurationProvider. Delegating keeps one implementation of each operation.

    async Task<IGenericResult<IGenericConfiguration>> IServiceConfigurationProvider.GetUntyped(Guid id, CancellationToken ct)
    {
        var result = await Get(id, ct).ConfigureAwait(false);
        return result.IsSuccess
            ? result.ToNewResult<IGenericConfiguration>(result.Value!)
            : result.ToNewResult<IGenericConfiguration>();
    }

    async Task<IGenericResult> IServiceConfigurationProvider.SaveUntyped(IGenericConfiguration record, CancellationToken ct)
    {
        if (record is not DataStoreConfiguration typed)
        {
            return GenericResult.Failure(
                DataStoreProviderLog.UntypedSaveTypeMismatch(
                    _logger, nameof(DataStoreConfiguration), record?.GetType().Name ?? "null"));
        }

        return await Save(typed, ct).ConfigureAwait(false);
    }

    Task<IGenericResult> IServiceConfigurationProvider.DeleteUntyped(Guid id, CancellationToken ct)
        => Delete(id, ct);
}
