using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Results;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for adding a container to an existing data store path.
/// POST datastores/{name}/containers
/// </summary>
public abstract class AddDataStoreContainerEndpointBase : CrudCreateEndpointBase<AddDataStoreContainerRequest, DataStoreContainerResponse>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <inheritdoc />
    protected AddDataStoreContainerEndpointBase(ILogger<AddDataStoreContainerEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Gets the nested-resource route.</summary>
    protected override string Route => "/datastores/{Name}/containers";

    /// <summary>Returns the container name as the resource identifier for duplicate-check logging.</summary>
    protected override string GetResourceName(AddDataStoreContainerRequest request) => request.ContainerName;

    /// <summary>
    /// Existence and duplicate-container checks are delegated to the provider's AddContainer method.
    /// Always returns false so the CrudCreateEndpointBase lifecycle proceeds to Create.
    /// </summary>
    protected override Task<IGenericResult<bool>> CheckExists(AddDataStoreContainerRequest request, CancellationToken ct)
        => Task.FromResult(GenericResult<bool>.Success(false));

    /// <summary>Delegates persistence to DataStoreConfigurationProvider.AddContainer, which enforces
    /// store-exists, path-exists, and no-duplicate-name invariants before writing.</summary>
    protected override async Task<IGenericResult<DataStoreContainerResponse>> Create(AddDataStoreContainerRequest request, CancellationToken ct)
    {
        var classified = DataClassificationValidator.Validate(request.Fields.Select(f => f.ClassificationId).Prepend(request.ClassificationId), Logger);
        if (classified.IsFailure) return classified.ToNewResult<DataStoreContainerResponse>();

        var typed = DatumTypeValidator.Validate(request.Fields.Select(f => f.DatumTypeId), Logger);
        if (typed.IsFailure) return typed.ToNewResult<DataStoreContainerResponse>();

        var dataTyped = DataTypeValidator.Validate(request.Fields.Select(f => f.FrameworkDataType), Logger);
        if (dataTyped.IsFailure) return dataTyped.ToNewResult<DataStoreContainerResponse>();

        var container = new DataContainerConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = request.ContainerName,
            TypeId = request.ContainerType,
            Format = request.Format,
            RecordSelector = request.RecordSelector,
            FlattenNestedObjects = request.FlattenNestedObjects,
            FlattenSeparator = request.FlattenSeparator,
            ClassificationId = request.ClassificationId,
            Fields = BuildFields(request.Fields),
        };

        // A container hangs from a path, which hangs from the store, so the write is a save of the
        // store carrying it. The store-exists and path-exists invariants the old AddContainer
        // enforced are stated here, where the rows they are about are in hand.
        var storeResult = await _dataStoreProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (!storeResult.IsSuccess || storeResult.Value is null)
            return GenericResult<DataStoreContainerResponse>.Failure(
                DataStoreEndpointLog.DataStoreNotFound(Logger, request.Name));

        var store = storeResult.Value;
        var path = store.Paths.FirstOrDefault(
            p => string.Equals(p.Name, request.PathName, StringComparison.OrdinalIgnoreCase));
        if (path is null)
            return GenericResult<DataStoreContainerResponse>.Failure(
                DataStoreEndpointLog.PathNotFoundInDataStore(Logger, request.PathName, request.Name));

        if (path.Containers.Any(c => string.Equals(c.Name, container.Name, StringComparison.OrdinalIgnoreCase)))
            return GenericResult<DataStoreContainerResponse>.Failure(
                DataStoreEndpointLog.ContainerAlreadyExists(Logger, container.Name, request.PathName, request.Name));

        var keysResult = BuildKeys(request, container, store);
        if (keysResult.IsFailure)
            return keysResult.ToNewResult<DataStoreContainerResponse>();
        container.Keys = keysResult.Value!;

        path.Containers.Add(container);

        var addResult = await _dataStoreProvider
            .Save(store, store.Domain, store.Implementation, store.Name, ct)
            .ConfigureAwait(false);
        if (addResult.IsFailure)
            return addResult.ToNewResult<DataStoreContainerResponse>();

        DataStoreEndpointLog.ContainerAdded(Logger, request.ContainerName, request.PathName, request.Name);

        return GenericResult<DataStoreContainerResponse>.Success(BuildResponse(container));
    }

    /// <summary>Builds the container's field rows from the request, pre-assigning each an Id.</summary>
    /// <remarks>
    /// A key can only name a field this same request defines, and it does so by field name -- the
    /// field has no Id yet anywhere the caller could have read one. Assigning it here, before
    /// BuildKeys runs, is what lets a key resolve that name to a real Id in the same call instead
    /// of needing a second round trip after the fields exist.
    /// </remarks>
    /// <summary>Protected, not private: exercised directly by tests without the provider dependency.</summary>
    protected static List<DataContainerFieldConfiguration> BuildFields(
        IEnumerable<CreateDataStoreContainerFieldRequest> fields) => DataStoreContainerBuilder.Fields(fields);

    /// <summary>Builds the create response from the saved container.</summary>
    private static DataStoreContainerResponse BuildResponse(DataContainerConfiguration container)
    {
        var dto = new DataStoreContainerResponse
        {
            Id = container.Id,
            Name = container.Name,
            ClassificationId = container.ClassificationId,
            Fields = container.Fields.Select(f => new DataStoreFieldResponse
            {
                Id = f.Id,
                Name = f.Name,
                NativeDataType = f.DataType,
                FrameworkDataType = f.FrameworkDataType,
                IsNullable = f.IsNullable,
                Ordinal = f.Ordinal,
                Description = f.Description,
                ClassificationId = f.ClassificationId,
                DatumTypeId = f.DatumTypeId,
                MaxLength = f.MaxLength,
                Precision = f.Precision,
                Scale = f.Scale,
            }).ToList(),
            SurrogateKeyFields = KeyFieldNames(container, "Surrogate"),
            NaturalKeyFields = KeyFieldNames(container, "Natural"),
        };
        if (container.TypeId is not null)
            dto.ContainerType = container.TypeId;

        return dto;
    }

    /// <summary>Builds the container's keys, resolving each key field and Foreign reference by name.</summary>
    /// <remarks>
    /// Why names, not Ids: the caller cannot know a field's Id before this same request creates it,
    /// and a Foreign key's referenced container/key are existing rows the caller identifies the way
    /// every other request in this endpoint family does -- by name, not by an internal Guid.
    /// </remarks>
    protected internal IGenericResult<List<DataContainerKeyConfiguration>> BuildKeys(
        AddDataStoreContainerRequest request, DataContainerConfiguration container, IDataStoreImplementationConfiguration store)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(store);

        return DataStoreContainerBuilder.Keys(
            request.Name, request.Keys, container, store.Paths.SelectMany(p => p.Containers), Logger);
    }

    /// <summary>Gets the names of the fields participating in the container's keys of the given type.</summary>
    /// <summary>Protected, not private: exercised directly by tests without the provider dependency.</summary>
    protected static List<string> KeyFieldNames(DataContainerConfiguration container, string keyTypeId) =>
        container.Keys
            .Where(k => string.Equals(k.TypeId, keyTypeId, StringComparison.OrdinalIgnoreCase))
            .SelectMany(k => k.KeyFields)
            .Select(kf => container.Fields.FirstOrDefault(f => f.Id == kf.DataContainerFieldId)?.Name)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToList();
}
