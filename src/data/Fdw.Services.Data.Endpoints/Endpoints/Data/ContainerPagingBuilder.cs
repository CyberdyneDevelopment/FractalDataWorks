using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads, checks, stores and reports the paging a container states for itself (FDW-5562), for every endpoint that carries it. The one place the request's
/// members become the container's, so add, create and set cannot disagree.
/// </summary>
/// <remarks>
/// What a style reads, and what it needs, is the store type's to say (<see cref="IDataStoreType.ContainerPaging"/>), so nothing here names a connection type.
/// </remarks>
internal static class ContainerPagingBuilder
{
    /// <summary>Finds a container of a data store by its durable id, with the store it is in.</summary>
    /// <param name="stores">The data store provider.</param>
    /// <param name="dataStoreName">The data store's name.</param>
    /// <param name="containerId">The container's durable id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The container and its store; success with none when the store has no such container; or a structured failure.</returns>
    internal static async Task<IGenericResult<ContainerLocation?>> Locate(
        IDataStoreConfigurationProvider stores, string dataStoreName, Guid containerId, CancellationToken ct)
    {
        var store = await stores.Get(dataStoreName, ct).ConfigureAwait(false);
        if (store.IsFailure)
            return store.ToNewResult<ContainerLocation?>();

        var container = store.Value?.Paths.SelectMany(path => path.Containers).FirstOrDefault(candidate => candidate.Id == containerId);
        return GenericResult<ContainerLocation?>.Success(store.Value is null || container is null ? null : new ContainerLocation(store.Value, container));
    }

    /// <summary>Reports what a container states for its own paging and the paging it reads with.</summary>
    /// <param name="stores">The data store provider.</param>
    /// <param name="connections">The connection provider.</param>
    /// <param name="found">The container and its store.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The report, or a structured failure.</returns>
    internal static async Task<IGenericResult<DataStoreContainerPagingDto>> Report(
        IDataStoreConfigurationProvider stores, IConnectionConfigurationProvider connections, ContainerLocation found, ILogger logger, CancellationToken ct)
    {
        var paging = await Detail(stores, connections, found.Store, found.Container, logger, ct).ConfigureAwait(false);
        if (paging.IsFailure)
            return paging.ToNewResult<DataStoreContainerPagingDto>();

        return GenericResult<DataStoreContainerPagingDto>.Success(new DataStoreContainerPagingDto
        {
            ContainerId = found.Container.Id,
            ContainerName = found.Container.Name,
            HasPagingOverride = !string.IsNullOrWhiteSpace(found.Container.PagingStyle),
            PagingStyle = found.Container.PagingStyle,
            PagingSettings = Own(found.Container),
            Paging = paging.Value,
        });
    }

    /// <summary>Gets the style a request names: trimmed, or null when it names none.</summary>
    /// <param name="style">The style the request names.</param>
    /// <returns>The style, or null.</returns>
    internal static string? StyleOf(string? style) => string.IsNullOrWhiteSpace(style) ? null : style.Trim();

    /// <summary>Gets the settings a request states: only those with a value, by name, trimmed. A blank value states nothing.</summary>
    /// <param name="settings">The settings the request sends, or null for none.</param>
    /// <returns>The settings stated.</returns>
    internal static Dictionary<string, string?> Stated(IDictionary<string, string?>? settings)
    {
        var stated = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (settings is null)
            return stated;

        foreach (var entry in settings)
        {
            if (!string.IsNullOrWhiteSpace(entry.Value))
                stated[entry.Key.Trim()] = entry.Value.Trim();
        }

        return stated;
    }

    /// <summary>Gets the settings a container states, by name.</summary>
    /// <param name="container">The container, with its paging settings read.</param>
    /// <returns>The settings it states.</returns>
    internal static Dictionary<string, string?> Own(DataContainerConfiguration container)
    {
        var own = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var setting in container.PagingSettings.Where(row => row.IsCurrent && !row.IsDeleted))
            own[setting.Name] = setting.Value;

        return own;
    }

    /// <summary>Refuses paging a container states that its store type or its connection cannot take, or that its style does not read.</summary>
    /// <param name="storeType">The type of the container's data store.</param>
    /// <param name="connection">The connection of the container's data store.</param>
    /// <param name="containerName">The container, for the refusal.</param>
    /// <param name="style">The style the container names, or null.</param>
    /// <param name="settings">The settings the container states.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The refusal; null when the container states no paging, or states paging that is valid.</returns>
    internal static IGenericResult? Refusal(
        IDataStoreType storeType,
        IConnectionImplementationConfiguration connection,
        string containerName,
        string? style,
        IReadOnlyDictionary<string, string?> settings,
        ILogger logger)
    {
        if (style is null && settings.Count == 0)
            return null;

        if (storeType.ContainerPaging is not { } rules)
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("ContainerPagingNotForStore"), logger,
                ResultDetails.Create("container", containerName, "storeType", storeType.Name));
        }

        var result = rules.Check(connection, containerName, style, settings, logger);
        return result.IsFailure ? result : null;
    }

    /// <summary>Gives a container the paging a request states, replacing what it stated before and keeping the identity of a setting it states again.</summary>
    /// <param name="container">The container, with its paging settings read.</param>
    /// <param name="style">The style it now names, or null for its connection's.</param>
    /// <param name="settings">The settings it now states.</param>
    internal static void Apply(DataContainerConfiguration container, string? style, IReadOnlyDictionary<string, string?> settings)
    {
        var kept = container.PagingSettings.Where(row => row.IsCurrent && !row.IsDeleted).ToList();
        container.PagingStyle = style;
        container.PagingSettings = [.. settings.Select(entry => new DataContainerPagingSettingConfiguration
        {
            Id = IdOf(kept, entry.Key),
            Name = entry.Key,
            Value = entry.Value ?? string.Empty,
        })];
    }

    // The identity a setting the container already states keeps across saves; a setting it did not state before is a new row.
    private static Guid IdOf(List<DataContainerPagingSettingConfiguration> kept, string name)
    {
        var existing = kept.FirstOrDefault(row => string.Equals(row.Name, name, StringComparison.OrdinalIgnoreCase));
        return existing is null ? Guid.CreateVersion7() : existing.Id;
    }

    /// <summary>Reads a container's paging settings without reading the rest of its data store.</summary>
    /// <param name="provider">The data store provider.</param>
    /// <param name="dataStoreName">The data store the container is in.</param>
    /// <param name="containerId">The container's durable id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The settings, or a structured failure.</returns>
    internal static async Task<IGenericResult<List<DataContainerPagingSettingConfiguration>>> Read(
        IDataStoreConfigurationProvider provider, string dataStoreName, Guid containerId, CancellationToken ct)
    {
        var settings = await provider.Get<DataContainerConfiguration, DataContainerPagingSettingConfiguration>(
            dataStoreName, containerId, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        return settings.IsFailure
            ? settings.ToNewResult<List<DataContainerPagingSettingConfiguration>>()
            : GenericResult<List<DataContainerPagingSettingConfiguration>>.Success([.. settings.Value!]);
    }

    /// <summary>Works out the paging a container of a data store reads with, for the container's detail: reads the container's settings and its store's connection.</summary>
    /// <param name="stores">The data store provider.</param>
    /// <param name="connections">The connection provider.</param>
    /// <param name="store">The container's data store, read as a record.</param>
    /// <param name="container">The container, read as a record; its paging settings are read into it.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The paging with where each value came from; success with none when the store's containers do not page or no style is stated at either level; or a structured failure.</returns>
    internal static async Task<IGenericResult<ContainerPagingDto?>> Detail(
        IDataStoreConfigurationProvider stores,
        IConnectionConfigurationProvider connections,
        IDataStoreImplementationConfiguration store,
        DataContainerConfiguration container,
        ILogger logger,
        CancellationToken ct)
    {
        var storeType = DataStoreTypes.ByName(store.Implementation);
        if (storeType.ContainerPaging is null)
            return GenericResult<ContainerPagingDto?>.Success(null);

        var connection = await connections.Get(store.ConnectionId, ct).ConfigureAwait(false);
        if (connection.IsFailure || connection.Value is null)
            return GenericResult<ContainerPagingDto?>.Failure(DataStoreEndpointLog.ConnectionNameUnresolved(logger, store.ConnectionId, store.Name));

        var settings = await Read(stores, store.Name, container.Id, ct).ConfigureAwait(false);
        if (settings.IsFailure)
            return settings.ToNewResult<ContainerPagingDto?>();

        container.PagingSettings = settings.Value!;
        return View(storeType, connection.Value, container, logger);
    }

    /// <summary>Works out the paging a container reads with, for the container's detail.</summary>
    /// <param name="storeType">The type of the container's data store.</param>
    /// <param name="connection">The connection of the container's data store.</param>
    /// <param name="container">The container, with its paging settings read.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The paging with where each value came from; success with none when the store does not page its containers or no style is stated at either level.</returns>
    internal static IGenericResult<ContainerPagingDto?> View(
        IDataStoreType storeType, IConnectionImplementationConfiguration connection, DataContainerConfiguration container, ILogger logger)
    {
        if (storeType.ContainerPaging is not { } rules)
            return GenericResult<ContainerPagingDto?>.Success(null);

        var resolved = rules.Resolve(connection, container.Name, container.PagingStyle, Own(container), logger);
        if (resolved.IsFailure)
            return resolved.ToNewResult<ContainerPagingDto?>();

        if (resolved.Value is not { } view)
            return GenericResult<ContainerPagingDto?>.Success(null);

        return GenericResult<ContainerPagingDto?>.Success(new ContainerPagingDto
        {
            Style = view.Style,
            StyleSource = view.StyleSource,
            Settings = [.. view.Settings.Select(value => new ContainerPagingValueDto { Name = value.Name, Value = value.Value, Source = value.Source })],
        });
    }
}
