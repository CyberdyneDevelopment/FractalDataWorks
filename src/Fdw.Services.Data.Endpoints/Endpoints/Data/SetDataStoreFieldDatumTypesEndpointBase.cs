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
/// Base endpoint for setting the datum type of one or more container fields of a data store (FDW-988).
/// </summary>
/// <remarks>
/// A store's containers and fields cannot be written one at a time, so this names the fields by durable id,
/// gives every one the datum type and saves the store once: typing a workbench selection writes one version
/// of the store. One field is a selection of one.
/// <para>
/// All or nothing: an id the store does not have refuses the request and nothing is changed.
/// </para>
/// </remarks>
public abstract class SetDataStoreFieldDatumTypesEndpointBase
    : CrudUpdateEndpointBase<SetDataStoreFieldDatumTypesRequest, List<DatumTypedFieldResponse>>
{
    private readonly DataStoreConfigurationProvider _dataStoreProvider;

    /// <summary>Initializes a new instance of the <see cref="SetDataStoreFieldDatumTypesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataStoreProvider">Reads and saves the data store.</param>
    protected SetDataStoreFieldDatumTypesEndpointBase(
        ILogger<SetDataStoreFieldDatumTypesEndpointBase> logger, DataStoreConfigurationProvider dataStoreProvider) : base(logger)
    {
        _dataStoreProvider = dataStoreProvider ?? throw new ArgumentNullException(nameof(dataStoreProvider));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastores";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/datum-types";

    /// <summary>Returns the store as the resource identifier.</summary>
    protected override string GetResourceIdentifier(SetDataStoreFieldDatumTypesRequest request) => request.Name;

    /// <summary>Finds the named fields; a store that does not exist answers 404, a field it lacks refuses the request.</summary>
    protected override async Task<IGenericResult<List<DatumTypedFieldResponse>?>> FindForUpdate(
        SetDataStoreFieldDatumTypesRequest request, CancellationToken ct)
    {
        var store = await _dataStoreProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (store.IsFailure) return store.ToNewResult<List<DatumTypedFieldResponse>?>();
        if (store.Value is null) return GenericResult<List<DatumTypedFieldResponse>?>.Success(null);

        var fields = Find(store.Value, request);
        return fields.IsFailure
            ? fields.ToNewResult<List<DatumTypedFieldResponse>?>()
            : GenericResult<List<DatumTypedFieldResponse>?>.Success(fields.Value!.Select(Describe).ToList());
    }

    /// <summary>Refuses a datum type the DatumTypes collection does not declare.</summary>
    protected override Task<IGenericResult> ValidateUpdate(
        SetDataStoreFieldDatumTypesRequest request, List<DatumTypedFieldResponse> existing, CancellationToken ct)
        => Task.FromResult(DatumTypeValidator.Validate(request.DatumTypeId, Logger));

    /// <summary>Sets every named field's datum type and saves the store once.</summary>
    /// <remarks>
    /// The fields are changed on the composed store before the save, and the composed rows may be the
    /// gateway's cached ones. A save that fails puts every old value back.
    /// </remarks>
    protected override async Task<IGenericResult<List<DatumTypedFieldResponse>>> Update(
        SetDataStoreFieldDatumTypesRequest request, List<DatumTypedFieldResponse> existing, CancellationToken ct)
    {
        var store = await _dataStoreProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (store.IsFailure) return store.ToNewResult<List<DatumTypedFieldResponse>>();
        if (store.Value is not { } current)
            return GenericResult<List<DatumTypedFieldResponse>>.Failure(
                DataStoreEndpointLog.DataStoreNotFound(Logger, request.Name));

        var fields = Find(current, request);
        if (fields.IsFailure) return fields.ToNewResult<List<DatumTypedFieldResponse>>();

        var previous = fields.Value!.Select(field => (field, field.DatumTypeId)).ToList();
        foreach (var field in fields.Value!)
            field.DatumTypeId = request.DatumTypeId;

        var saved = await _dataStoreProvider.Update(current, "DataStore", current.Implementation, current.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            foreach (var (field, datumTypeId) in previous)
                field.DatumTypeId = datumTypeId;
            return saved.ToNewResult<List<DatumTypedFieldResponse>>();
        }

        return GenericResult<List<DatumTypedFieldResponse>>.Success(fields.Value!.Select(Describe).ToList());
    }

    // Every field the request names, or the failure naming the ones the store does not have. Field ids are
    // unique across the store's containers; a repeated id is one field.
    private IGenericResult<IReadOnlyList<DataContainerFieldConfiguration>> Find(IDataStoreImplementationConfiguration store, SetDataStoreFieldDatumTypesRequest request)
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

    private static DatumTypedFieldResponse Describe(DataContainerFieldConfiguration field) => new()
    {
        Id = field.Id,
        Name = field.Name,
        DatumTypeId = field.DatumTypeId
    };
}
