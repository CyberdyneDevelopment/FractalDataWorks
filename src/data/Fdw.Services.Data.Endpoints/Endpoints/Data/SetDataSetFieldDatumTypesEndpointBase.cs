using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for setting the datum type of one or more fields of a data set (FDW-988).
/// </summary>
/// <remarks>
/// A data set's fields can be written only by replacing all of them. This names the fields by durable id,
/// gives every one the datum type and saves the data set once, so typing a selection writes one version of
/// the data set. One field is a selection of one.
/// <para>
/// All or nothing: an id the data set does not have refuses the request and nothing is changed.
/// </para>
/// </remarks>
public abstract class SetDataSetFieldDatumTypesEndpointBase
    : CrudUpdateEndpointBase<SetDataSetFieldDatumTypesRequest, List<DatumTypedFieldResponse>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="SetDataSetFieldDatumTypesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and saves the data set.</param>
    protected SetDataSetFieldDatumTypesEndpointBase(
        ILogger<SetDataSetFieldDatumTypesEndpointBase> logger, DataSetConfigurationProvider dataSetProvider) : base(logger)
    {
        _dataSetProvider = dataSetProvider ?? throw new ArgumentNullException(nameof(dataSetProvider));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/fields/datum-type";

    /// <summary>Returns the data set as the resource identifier.</summary>
    protected override string GetResourceIdentifier(SetDataSetFieldDatumTypesRequest request) => request.Name;

    /// <summary>Finds the named fields; a data set that does not exist answers 404, a field it lacks refuses the request.</summary>
    protected override async Task<IGenericResult<List<DatumTypedFieldResponse>?>> FindForUpdate(
        SetDataSetFieldDatumTypesRequest request, CancellationToken ct)
    {
        var dataSet = await _dataSetProvider.Get(request.Name, ConfigurationLevels.Children, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<List<DatumTypedFieldResponse>?>();
        if (dataSet.Value is null) return GenericResult<List<DatumTypedFieldResponse>?>.Success(null);

        var fields = Find(dataSet.Value, request);
        return fields.IsFailure
            ? fields.ToNewResult<List<DatumTypedFieldResponse>?>()
            : GenericResult<List<DatumTypedFieldResponse>?>.Success(fields.Value!.Select(Describe).ToList());
    }

    /// <summary>Refuses a datum type the DatumTypes collection does not declare.</summary>
    protected override Task<IGenericResult> ValidateUpdate(
        SetDataSetFieldDatumTypesRequest request, List<DatumTypedFieldResponse> existing, CancellationToken ct)
        => Task.FromResult(DatumTypeValidator.Validate(request.DatumTypeId, Logger));

    /// <summary>Sets every named field's datum type and saves the data set once.</summary>
    /// <remarks>
    /// The fields are changed on the composed data set before the save, and the composed rows may be the
    /// gateway's cached ones. A save that fails puts every old value back.
    /// </remarks>
    protected override async Task<IGenericResult<List<DatumTypedFieldResponse>>> Update(
        SetDataSetFieldDatumTypesRequest request, List<DatumTypedFieldResponse> existing, CancellationToken ct)
    {
        var dataSet = await _dataSetProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<List<DatumTypedFieldResponse>>();
        if (dataSet.Value is not { } current)
            return GenericResult<List<DatumTypedFieldResponse>>.Failure(
                DataSetEndpointLog.DataSetNotFound(Logger, request.Name));

        var fields = Find(current, request);
        if (fields.IsFailure) return fields.ToNewResult<List<DatumTypedFieldResponse>>();

        var previous = fields.Value!.Select(field => (field, field.DatumTypeId)).ToList();
        foreach (var field in fields.Value!)
            field.DatumTypeId = request.DatumTypeId;

        var saved = await _dataSetProvider.Update(current, "DataSet", current.Implementation, current.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            foreach (var (field, datumTypeId) in previous)
                field.DatumTypeId = datumTypeId;
            return saved.ToNewResult<List<DatumTypedFieldResponse>>();
        }

        return GenericResult<List<DatumTypedFieldResponse>>.Success(fields.Value!.Select(Describe).ToList());
    }

    // Every field the request names, or the failure naming the ones the data set does not have. A
    // repeated id is one field.
    private IGenericResult<IReadOnlyList<DataSetFieldConfiguration>> Find(IDataSetImplementationConfiguration dataSet, SetDataSetFieldDatumTypesRequest request)
    {
        var wanted = request.FieldIds.Distinct().ToList();
        if (wanted.Count == 0)
            return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Failure(
                DataStoreEndpointLog.BulkTargetsMissing(Logger, "type", "field", dataSet.Name));

        var byId = dataSet.Fields.ToDictionary(field => field.Id);
        var missing = wanted.Where(id => !byId.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Failure(
                DataStoreEndpointLog.BulkTargetsNotFound(Logger, "type", dataSet.Name, "field", string.Join(", ", missing)));

        return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Success(wanted.Select(id => byId[id]).ToList());
    }

    private static DatumTypedFieldResponse Describe(DataSetFieldConfiguration field) => new()
    {
        Id = field.Id,
        Name = field.Name,
        DatumTypeId = field.DatumTypeId
    };
}
