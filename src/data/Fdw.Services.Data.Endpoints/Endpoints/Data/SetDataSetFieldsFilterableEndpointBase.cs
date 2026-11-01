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
/// Base endpoint for choosing whether API callers may filter on one or more fields of a data set (FDW-1048).
/// </summary>
/// <remarks>
/// Written the way <see cref="ClassifyDataSetFieldsEndpointBase"/> is: the fields are named by durable id, every one
/// is set and the data set is saved once, so a selection is one version of the data set. All or nothing: an id the data
/// set does not have refuses the request and nothing is changed.
/// </remarks>
public abstract class SetDataSetFieldsFilterableEndpointBase
    : CrudUpdateEndpointBase<SetDataSetFieldsFilterableRequest, List<FilterableFieldResponse>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="SetDataSetFieldsFilterableEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and saves the data set.</param>
    protected SetDataSetFieldsFilterableEndpointBase(
        ILogger<SetDataSetFieldsFilterableEndpointBase> logger, DataSetConfigurationProvider dataSetProvider) : base(logger)
    {
        _dataSetProvider = dataSetProvider ?? throw new ArgumentNullException(nameof(dataSetProvider));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/fields/filterable";

    /// <summary>Returns the data set as the resource identifier.</summary>
    protected override string GetResourceIdentifier(SetDataSetFieldsFilterableRequest request) => request.Name;

    /// <summary>Finds the named fields; a data set that does not exist answers 404, a field it lacks refuses the request.</summary>
    protected override async Task<IGenericResult<List<FilterableFieldResponse>?>> FindForUpdate(
        SetDataSetFieldsFilterableRequest request, CancellationToken ct)
    {
        var dataSet = await _dataSetProvider.Get(request.Name, ConfigurationLevels.Children, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<List<FilterableFieldResponse>?>();
        if (dataSet.Value is null) return GenericResult<List<FilterableFieldResponse>?>.Success(null);

        var fields = Find(dataSet.Value, request);
        return fields.IsFailure
            ? fields.ToNewResult<List<FilterableFieldResponse>?>()
            : GenericResult<List<FilterableFieldResponse>?>.Success(fields.Value!.Select(Describe).ToList());
    }

    /// <summary>Sets every named field's choice and saves the data set once.</summary>
    /// <remarks>
    /// The fields are changed on the composed data set before the save, and the composed rows may be the
    /// gateway's cached ones. A save that fails puts every old value back.
    /// </remarks>
    protected override async Task<IGenericResult<List<FilterableFieldResponse>>> Update(
        SetDataSetFieldsFilterableRequest request, List<FilterableFieldResponse> existing, CancellationToken ct)
    {
        var dataSet = await _dataSetProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<List<FilterableFieldResponse>>();
        if (dataSet.Value is not { } current)
            return GenericResult<List<FilterableFieldResponse>>.Failure(
                DataSetEndpointLog.DataSetNotFound(Logger, request.Name));

        var fields = Find(current, request);
        if (fields.IsFailure) return fields.ToNewResult<List<FilterableFieldResponse>>();

        var previous = fields.Value!.Select(field => (field, field.IsFilterable)).ToList();
        foreach (var field in fields.Value!)
            field.IsFilterable = request.IsFilterable;

        var saved = await _dataSetProvider.Update(current, "DataSet", current.Implementation, current.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            foreach (var (field, isFilterable) in previous)
                field.IsFilterable = isFilterable;
            return saved.ToNewResult<List<FilterableFieldResponse>>();
        }

        return GenericResult<List<FilterableFieldResponse>>.Success(fields.Value!.Select(Describe).ToList());
    }

    // Every field the request names, or the failure naming the ones the data set does not have. A
    // repeated id is one field.
    private IGenericResult<IReadOnlyList<DataSetFieldConfiguration>> Find(IDataSetImplementationConfiguration dataSet, SetDataSetFieldsFilterableRequest request)
    {
        var wanted = request.FieldIds.Distinct().ToList();
        if (wanted.Count == 0)
            return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Failure(
                DataStoreEndpointLog.BulkTargetsMissing(Logger, "filterable", "field", dataSet.Name));

        var byId = dataSet.Fields.ToDictionary(field => field.Id);
        var missing = wanted.Where(id => !byId.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Failure(
                DataStoreEndpointLog.BulkTargetsNotFound(Logger, "filterable", dataSet.Name, "field", string.Join(", ", missing)));

        return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Success(wanted.Select(id => byId[id]).ToList());
    }

    private static FilterableFieldResponse Describe(DataSetFieldConfiguration field) => new()
    {
        Id = field.Id,
        Name = field.Name,
        IsFilterable = field.IsFilterable
    };
}
