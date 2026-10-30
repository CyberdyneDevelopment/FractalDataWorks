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
/// Base endpoint for setting the data classification of one or more fields of a data set (FDW-938, FDW-960).
/// </summary>
/// <remarks>
/// A data set's fields can be written only by replacing all of them. This names the fields by durable
/// id, sets every one to the level and saves the data set once, so classifying a selection writes one
/// version of the data set. One field is a selection of one.
/// <para>
/// All or nothing: an id the data set does not have refuses the request and nothing is changed.
/// </para>
/// </remarks>
public abstract class ClassifyDataSetFieldsEndpointBase
    : CrudUpdateEndpointBase<ClassifyDataSetFieldsRequest, List<ClassifiedItemResponse>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="ClassifyDataSetFieldsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and saves the data set.</param>
    protected ClassifyDataSetFieldsEndpointBase(
        ILogger<ClassifyDataSetFieldsEndpointBase> logger, DataSetConfigurationProvider dataSetProvider) : base(logger)
    {
        _dataSetProvider = dataSetProvider ?? throw new ArgumentNullException(nameof(dataSetProvider));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => $"/{ResourceName}/{{Name}}/fields/classification";

    /// <summary>Returns the data set as the resource identifier.</summary>
    protected override string GetResourceIdentifier(ClassifyDataSetFieldsRequest request) => request.Name;

    /// <summary>Finds the named fields; a data set that does not exist answers 404, a field it lacks refuses the request.</summary>
    protected override async Task<IGenericResult<List<ClassifiedItemResponse>?>> FindForUpdate(
        ClassifyDataSetFieldsRequest request, CancellationToken ct)
    {
        var dataSet = await _dataSetProvider.Get(request.Name, ConfigurationLevels.Children, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<List<ClassifiedItemResponse>?>();
        if (dataSet.Value is null) return GenericResult<List<ClassifiedItemResponse>?>.Success(null);

        var fields = Find(dataSet.Value, request);
        return fields.IsFailure
            ? fields.ToNewResult<List<ClassifiedItemResponse>?>()
            : GenericResult<List<ClassifiedItemResponse>?>.Success(fields.Value!.Select(Describe).ToList());
    }

    /// <summary>Refuses a classification that names no level.</summary>
    protected override Task<IGenericResult> ValidateUpdate(
        ClassifyDataSetFieldsRequest request, List<ClassifiedItemResponse> existing, CancellationToken ct)
        => Task.FromResult(DataClassificationValidator.Validate(request.ClassificationId, Logger));

    /// <summary>Sets every named field's classification and saves the data set once.</summary>
    /// <remarks>
    /// The fields are changed on the composed data set before the save, and the composed rows may be the
    /// gateway's cached ones. A save that fails puts every old value back.
    /// </remarks>
    protected override async Task<IGenericResult<List<ClassifiedItemResponse>>> Update(
        ClassifyDataSetFieldsRequest request, List<ClassifiedItemResponse> existing, CancellationToken ct)
    {
        var dataSet = await _dataSetProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<List<ClassifiedItemResponse>>();
        if (dataSet.Value is not { } current)
            return GenericResult<List<ClassifiedItemResponse>>.Failure(
                DataSetEndpointLog.DataSetNotFound(Logger, request.Name));

        var fields = Find(current, request);
        if (fields.IsFailure) return fields.ToNewResult<List<ClassifiedItemResponse>>();

        var previous = fields.Value!.Select(field => (field, field.ClassificationId)).ToList();
        foreach (var field in fields.Value!)
            field.ClassificationId = request.ClassificationId;

        var saved = await _dataSetProvider.Update(current, "DataSet", current.Implementation, current.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            foreach (var (field, classificationId) in previous)
                field.ClassificationId = classificationId;
            return saved.ToNewResult<List<ClassifiedItemResponse>>();
        }

        return GenericResult<List<ClassifiedItemResponse>>.Success(fields.Value!.Select(Describe).ToList());
    }

    // Every field the request names, or the failure naming the ones the data set does not have. A
    // repeated id is one field.
    private IGenericResult<IReadOnlyList<DataSetFieldConfiguration>> Find(IDataSetImplementationConfiguration dataSet, ClassifyDataSetFieldsRequest request)
    {
        var wanted = request.FieldIds.Distinct().ToList();
        if (wanted.Count == 0)
            return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Failure(
                DataStoreEndpointLog.BulkTargetsMissing(Logger, "classify", "field", dataSet.Name));

        var byId = dataSet.Fields.ToDictionary(field => field.Id);
        var missing = wanted.Where(id => !byId.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Failure(
                DataStoreEndpointLog.BulkTargetsNotFound(Logger, "classify", dataSet.Name, "field", string.Join(", ", missing)));

        return GenericResult<IReadOnlyList<DataSetFieldConfiguration>>.Success(wanted.Select(id => byId[id]).ToList());
    }

    private static ClassifiedItemResponse Describe(DataSetFieldConfiguration field) => new()
    {
        Id = field.Id,
        Kind = "Field",
        Name = field.Name,
        ClassificationId = field.ClassificationId
    };
}
