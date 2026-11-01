using System;
using System.Linq;
using System.Collections.Generic;
using Fdw.Data.Abstractions.Results;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Validation;
using Fdw.Services.Connections.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new data store configuration.
/// </summary>
/// <typeparam name="TConfig">The concrete data store configuration type.</typeparam>
public abstract class CreateDataStoreEndpointBase<TConfig> : CrudCreateEndpointBase<CreateDataStoreRequest, DataStoreDetailResponse>
    where TConfig : IDataStoreImplementationConfiguration
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;
    private readonly ConnectionConfigurationProvider _connectionProvider;
    private readonly ISchemaInformationService _schema;

    /// <summary>Initializes a new instance of the <see cref="CreateDataStoreEndpointBase{TConfig}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Saves and reads the store.</param>
    /// <param name="connectionProvider">Resolves the store's connection by name.</param>
    /// <param name="schema">Discovers the connection's schema when the store is created with <c>discover</c>.</param>
    protected CreateDataStoreEndpointBase(ILogger<CreateDataStoreEndpointBase<TConfig>> logger, DataStoreConfigurationProvider dataStoreProvider,
        ConnectionConfigurationProvider connectionProvider, ISchemaInformationService schema) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
        _connectionProvider = connectionProvider;
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Returns the data store name from the create request.</summary>
    protected override string GetResourceName(CreateDataStoreRequest request) => request.Name;

    /// <summary>Checks whether a data store with the requested name already exists.</summary>
    protected override async Task<IGenericResult<bool>> CheckExists(CreateDataStoreRequest request, CancellationToken ct)
    {
        var existResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existResult.IsSuccess && existResult.Value != null);
    }

    /// <summary>Creates the data store configuration and persists it via IConfigurationWriter.</summary>
    protected override async Task<IGenericResult<DataStoreDetailResponse>> Create(CreateDataStoreRequest request, CancellationToken ct)
    {
        var classified = DataClassificationValidator.Validate(ClassificationsNamedBy(request), Logger);
        if (classified.IsFailure) return classified.ToNewResult<DataStoreDetailResponse>();

        // A store created with no paths declares no fields, so it names no datum types to check.
        var typed = request.Paths is null
            ? GenericResult.Success()
            : DatumTypeValidator.Validate(
                request.Paths.SelectMany(p => p.Containers).SelectMany(c => c.Fields).Select(f => f.DatumTypeId), Logger);
        if (typed.IsFailure) return typed.ToNewResult<DataStoreDetailResponse>();

        var dataTyped = request.Paths is null
            ? GenericResult.Success()
            : DataTypeValidator.Validate(
                request.Paths.SelectMany(p => p.Containers).SelectMany(c => c.Fields).Select(f => f.FrameworkDataType), Logger);
        if (dataTyped.IsFailure) return dataTyped.ToNewResult<DataStoreDetailResponse>();

        var connectionResult = await _connectionProvider.Get(request.ConnectionName, ct).ConfigureAwait(false);
        if (!connectionResult.IsSuccess || connectionResult.Value is null)
        {
            return GenericResult<DataStoreDetailResponse>.Failure(
                DataStoreEndpointLog.ConnectionNotFound(Logger, request.ConnectionName, request.Name));
        }

        var connection = connectionResult.Value;
        var storeType = DataStoreTypeSelection.Select(request, connection.Implementation, Logger);
        if (storeType.IsFailure) return storeType.ToNewResult<DataStoreDetailResponse>();

        var config = CreateConfiguration(request, connection.Id);
        config.ClassificationId = request.ClassificationId;
        config.CreatedAt = DateTimeOffset.UtcNow;

        var tree = ApplyTree(config, request.Paths, storeType.Value!);
        if (tree.IsFailure)
        {
            return tree.ToNewResult<DataStoreDetailResponse>();
        }

        if (request.Discover)
        {
            var imported = await _schema.DiscoverInto(
                config, DataStoreDiscoveryOptions.Default, refresh: false, ct).ConfigureAwait(false);
            if (imported.IsFailure)
            {
                return imported.ToNewResult<DataStoreDetailResponse>();
            }
        }

        config.Implementation = storeType.Value!;

        var saveResult = await _dataStoreProvider.Save(config, "DataStore", config.Implementation, config.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            return saveResult.ToNewResult<DataStoreDetailResponse>();
        }

        var loadResult = await _dataStoreProvider.Get(config.Name, ct).ConfigureAwait(false);
        if (loadResult.IsSuccess && loadResult.Value != null)
        {
            config.Paths = loadResult.Value.Paths;
        }
        else
        {
            DataStoreEndpointLog.CacheReloadFailed(Logger, config.Name);
        }

        return GenericResult<DataStoreDetailResponse>.Success(MapToDetail(config, request));
    }

    /// <summary>Builds a concrete data store configuration from the create request. Override for type-specific fields.</summary>
    protected abstract TConfig CreateConfiguration(CreateDataStoreRequest request, Guid connectionId);

    /// <summary>Puts the requested tree - paths, their containers, and each container's fields and keys - on the configuration before it is saved.</summary>
    /// <param name="target">The configuration about to be saved.</param>
    /// <param name="paths">The paths the caller asked for, if any.</param>
    /// <param name="storeTypeName">The data store type the store is being created as, for the rules of how its containers are addressed.</param>
    /// <returns>Success, or the first container the store cannot take or key that could not be resolved.</returns>
    /// <remarks>
    /// Keys are resolved in two passes over every container in the request - the other keys first, then
    /// the Foreign ones - so a Foreign key may reference a key of any container in the same request,
    /// wherever it sits in the tree (FDW-920).
    /// </remarks>
    protected IGenericResult ApplyTree(TConfig target, IList<CreateDataStorePathRequest>? paths, string storeTypeName)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (paths is null || paths.Count == 0)
        {
            return GenericResult.Success();
        }

        if (ContainerRefusal(paths, DataStoreTypes.ByName(storeTypeName).AddressesContainersAsFiles) is { } refusal)
        {
            return refusal;
        }

        var containers = BuildPaths(target, paths);
        var all = containers.Select(c => c.Container).ToList();

        foreach (var foreignPass in new[] { false, true })
        {
            foreach (var (request, container) in containers)
            {
                var keys = DataStoreContainerBuilder.Keys(
                    target.Name,
                    request.Keys.Where(k => DataStoreContainerBuilder.IsForeign(k) == foreignPass),
                    container,
                    all,
                    Logger);
                if (keys.IsFailure || keys.Value is not { } built)
                {
                    return keys;
                }

                container.Keys.AddRange(built);
            }
        }

        DataStoreEndpointLog.TreeBuiltFromRequest(
            Logger, target.Name, target.Paths.Count, all.Count, all.Sum(c => c.Fields.Count));
        return GenericResult.Success();
    }

    // Every classification the request names -- the store's and each path's, container's and field's --
    // so one unknown id refuses the whole create before anything is written.
    private static IEnumerable<int> ClassificationsNamedBy(CreateDataStoreRequest request)
    {
        yield return request.ClassificationId;
        if (request.Paths is null)
            yield break;

        foreach (var path in request.Paths)
        {
            yield return path.ClassificationId;
            foreach (var container in path.Containers)
            {
                yield return container.ClassificationId;
                foreach (var field in container.Fields)
                    yield return field.ClassificationId;
            }
        }
    }

    // The first container of the tree that states a file option its format does not take, or is named with its
    // format's own extension in a store that addresses files; null when every container is one the store can read.
    private IGenericResult? ContainerRefusal(IList<CreateDataStorePathRequest> paths, bool storeAddressesContainersAsFiles)
    {
        foreach (var container in paths.SelectMany(p => p.Containers))
        {
            var options = DataStoreContainerBuilder.FileOptionsRefusal(
                container.Name, container.Format, container.HasHeader, container.Separator, container.SheetName, Logger);
            if (options is not null)
            {
                return options;
            }

            var target = DataStoreContainerWriteTarget.Refusal(
                container.Name, container.WriteEndpoint, container.WriteMethod, container.WriteContentType, Logger);
            if (target is not null)
            {
                return target;
            }

            var named = DataStoreContainerBuilder.NameRefusal(container.Name, container.Format, storeAddressesContainersAsFiles, Logger);
            if (named is not null)
            {
                return named;
            }
        }

        return null;
    }

    private static List<(CreateDataStorePathContainerRequest Request, DataContainerConfiguration Container)> BuildPaths(
        TConfig target, IList<CreateDataStorePathRequest> paths)
    {
        var containers = new List<(CreateDataStorePathContainerRequest Request, DataContainerConfiguration Container)>();
        target.Paths = [];
        foreach (var pathRequest in paths)
        {
            var path = new DataPathConfiguration
            {
                Name = pathRequest.Name,
                PathValue = pathRequest.PhysicalPath,
                Description = pathRequest.Description,
                ClassificationId = pathRequest.ClassificationId,
            };

            foreach (var containerRequest in pathRequest.Containers)
            {
                var container = new DataContainerConfiguration
                {
                    Id = Guid.CreateVersion7(),
                    Name = containerRequest.Name,
                    TypeId = containerRequest.ContainerType,
                    Format = containerRequest.Format,
                    RecordSelector = containerRequest.RecordSelector,
                    FlattenNestedObjects = containerRequest.FlattenNestedObjects,
                    FlattenSeparator = containerRequest.FlattenSeparator,
                    HasHeader = containerRequest.HasHeader,
                    Separator = containerRequest.Separator,
                    SheetName = containerRequest.SheetName,
                    WriteEndpoint = containerRequest.WriteEndpoint,
                    WriteMethod = containerRequest.WriteMethod,
                    WriteContentType = containerRequest.WriteContentType,
                    ClassificationId = containerRequest.ClassificationId,
                    Fields = DataStoreContainerBuilder.Fields(containerRequest.Fields),
                };
                path.Containers.Add(container);
                containers.Add((containerRequest, container));
            }

            target.Paths.Add(path);
        }

        return containers;
    }
    /// <summary>Maps the saved configuration to a detail DTO. Override for type-specific fields.</summary>
    protected abstract DataStoreDetailResponse MapToDetail(TConfig savedConfig, CreateDataStoreRequest request);

    /// <summary>Sends a 201 Created response with the data store detail.</summary>
    protected override Task SendCreatedResponse(DataStoreDetailResponse detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }

}
