using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for setting the portable data type of one or more container fields of a data store (FDW-1103).
/// </summary>
/// <remarks>
/// A store's containers and fields cannot be written one at a time, so this names the fields by durable id,
/// gives every one the portable data type and saves the store once: typing a workbench selection writes one version
/// of the store. One field is a selection of one.
/// <para>
/// All or nothing: an id the store does not have refuses the request and nothing is changed.
/// </para>
/// </remarks>
public abstract class SetDataStoreFieldDataTypesEndpointBase
    : CrudUpdateEndpointBase<SetDataStoreFieldDataTypesRequest, List<DataTypedFieldResponse>>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <summary>Initializes a new instance of the <see cref="SetDataStoreFieldDataTypesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads and saves the data store.</param>
    protected SetDataStoreFieldDataTypesEndpointBase(
        ILogger<SetDataStoreFieldDataTypesEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider ?? throw new ArgumentNullException(nameof(dataStoreProvider));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/data-types";

    /// <summary>Returns the store as the resource identifier.</summary>
    protected override string GetResourceIdentifier(SetDataStoreFieldDataTypesRequest request) => request.Name;

    /// <summary>Finds the named fields; a store that does not exist answers 404, a field it lacks refuses the request.</summary>
    protected override async Task<IGenericResult<List<DataTypedFieldResponse>?>> FindForUpdate(
        SetDataStoreFieldDataTypesRequest request, CancellationToken ct)
    {
        var store = await _dataStoreProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (store.IsFailure) return store.ToNewResult<List<DataTypedFieldResponse>?>();
        if (store.Value is null) return GenericResult<List<DataTypedFieldResponse>?>.Success(null);

        var fields = Find(store.Value, request);
        return fields.IsFailure
            ? fields.ToNewResult<List<DataTypedFieldResponse>?>()
            : GenericResult<List<DataTypedFieldResponse>?>.Success(fields.Value!.Select(Describe).ToList());
    }

    /// <summary>Refuses a data type the DataTypes collection does not declare.</summary>
    protected override Task<IGenericResult> ValidateUpdate(
        SetDataStoreFieldDataTypesRequest request, List<DataTypedFieldResponse> existing, CancellationToken ct)
        => Task.FromResult(DataTypeValidator.Validate(request.FrameworkDataType, Logger));

    /// <summary>Sets every named field's portable data type and saves the store once.</summary>
    /// <remarks>
    /// The fields are changed on the composed store before the save, and the composed rows may be the
    /// gateway's cached ones. A save that fails puts every old value back.
    /// </remarks>
    protected override async Task<IGenericResult<List<DataTypedFieldResponse>>> Update(
        SetDataStoreFieldDataTypesRequest request, List<DataTypedFieldResponse> existing, CancellationToken ct)
    {
        var store = await _dataStoreProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (store.IsFailure) return store.ToNewResult<List<DataTypedFieldResponse>>();
        if (store.Value is not { } current)
            return GenericResult<List<DataTypedFieldResponse>>.Failure(
                DataStoreEndpointLog.DataStoreNotFound(Logger, request.Name));

        var fields = Find(current, request);
        if (fields.IsFailure) return fields.ToNewResult<List<DataTypedFieldResponse>>();

        var previous = fields.Value!.Select(field => (field, field.FrameworkDataType)).ToList();
        foreach (var field in fields.Value!)
            field.FrameworkDataType = request.FrameworkDataType;

        var saved = await _dataStoreProvider.Update(current, "DataStore", current.Implementation, current.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            foreach (var (field, frameworkDataType) in previous)
                field.FrameworkDataType = frameworkDataType;
            return saved.ToNewResult<List<DataTypedFieldResponse>>();
        }

        return GenericResult<List<DataTypedFieldResponse>>.Success(fields.Value!.Select(Describe).ToList());
    }

    // Every field the request names, or the failure naming the ones the store does not have. Field ids are
    // unique across the store's containers; a repeated id is one field.
    private IGenericResult<IReadOnlyList<DataContainerFieldConfiguration>> Find(IDataStoreImplementationConfiguration store, SetDataStoreFieldDataTypesRequest request)
    {
        var wanted = request.FieldIds.Distinct().ToList();
        if (wanted.Count == 0)
            return GenericResult<IReadOnlyList<DataContainerFieldConfiguration>>.Failure(
                DataStoreEndpointLog.BulkTargetsMissing(Logger, "type", "field", store.Name));

        var byId = store.Paths
            .SelectMany(path => path.Containers)
            .SelectMany(container => container.Fields)
            .ToDictionary(field => field.Id);
        var missing = wanted.Where(id => !byId.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return GenericResult<IReadOnlyList<DataContainerFieldConfiguration>>.Failure(
                DataStoreEndpointLog.BulkTargetsNotFound(Logger, "type", store.Name, "field", string.Join(", ", missing)));

        return GenericResult<IReadOnlyList<DataContainerFieldConfiguration>>.Success(wanted.Select(id => byId[id]).ToList());
    }

    private static DataTypedFieldResponse Describe(DataContainerFieldConfiguration field) => new()
    {
        Id = field.Id,
        Name = field.Name,
        FrameworkDataType = field.FrameworkDataType
    };
}
