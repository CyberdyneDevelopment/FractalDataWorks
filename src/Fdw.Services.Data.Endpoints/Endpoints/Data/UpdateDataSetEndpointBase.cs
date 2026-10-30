using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions.Results;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing data set configuration.
/// Delegates all reads and writes to DataSetConfigurationProvider.
/// </summary>
public abstract class UpdateDataSetEndpointBase : CrudUpdateEndpointBase<UpdateDataSetRequest, DataSetDetailResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly CategoryConfigurationProvider _categories;

    /// <inheritdoc />
    protected UpdateDataSetEndpointBase(
        ILogger<UpdateDataSetEndpointBase> logger, DataSetConfigurationProvider dataSetProvider, CategoryConfigurationProvider categories) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <summary>Returns the data set name from the update request.</summary>
    protected override string GetResourceIdentifier(UpdateDataSetRequest request) => request.Name;

    /// <summary>Finds the existing data set configuration to update.</summary>
    protected override Task<IGenericResult<DataSetDetailResponse?>> FindForUpdate(UpdateDataSetRequest request, CancellationToken ct)
    {
        return LoadDataSetDetail(request.Name, ct);
    }

    /// <summary>Updates the data set configuration and persists it.</summary>
    protected override Task<IGenericResult<DataSetDetailResponse>> Update(
        UpdateDataSetRequest request,
        DataSetDetailResponse existing,
        CancellationToken ct)
    {
        return SaveUpdatedDataSet(request, ct);
    }

    /// <summary>
    /// Loads a data set by name and returns as detail DTO.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<DataSetDetailResponse?>> LoadDataSetDetail(string name, CancellationToken ct)
    {
        DataSetEndpointLog.LoadingDataSet(Logger, name, string.Empty);

        var result = await _dataSetProvider.Get(name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<DataSetDetailResponse?>();

        if (result.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, name);
            return GenericResult<DataSetDetailResponse?>.Success((DataSetDetailResponse?)null);
        }

        return await Detail(result.Value, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads the current configuration, merges the updates, and saves.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<DataSetDetailResponse>> SaveUpdatedDataSet(UpdateDataSetRequest request, CancellationToken ct)
    {
        DataSetEndpointLog.UpdatingDataSet(Logger, request.Name);

        var loadResult = await _dataSetProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (loadResult.IsFailure) return loadResult.ToNewResult<DataSetDetailResponse>();

        if (loadResult.Value is null)
            return GenericResult<DataSetDetailResponse>.Failure(DataSetEndpointLog.DataSetNotFound(Logger, request.Name));

        var existing = loadResult.Value;

        var mergedImplementation = request.Implementation ?? existing.Implementation;
        if (string.IsNullOrWhiteSpace(mergedImplementation)
            || ReferenceEquals(DataSetTypes.ByName(mergedImplementation), DataSetTypes.NotFound))
        {
            return GenericResult<DataSetDetailResponse>.Failure(
                DataSetsResultCodes.ImplementationInvalid, Logger,
                ImplementationInvalidCode.Describe(request.Name, mergedImplementation));
        }

        var kindValidation = ValidateKind(request, existing, mergedImplementation);
        if (kindValidation.IsFailure) return kindValidation.ToNewResult<DataSetDetailResponse>();

        var mergedFederationStrategy = request.FederationStrategy ?? existing.FederationStrategyOrNone();
        var federationValidation = DataSetQueryHelper.ValidateFederationStrategy(
            mergedImplementation, mergedFederationStrategy, request.Name, Logger);
        if (federationValidation.IsFailure) return federationValidation.ToNewResult<DataSetDetailResponse>();

        var childrenValidation = ValidateRequestedChildren(request);
        if (childrenValidation.IsFailure) return childrenValidation.ToNewResult<DataSetDetailResponse>();

        var category = await RequestedCategory(request, existing, ct).ConfigureAwait(false);
        if (category.IsFailure) return category.ToNewResult<DataSetDetailResponse>();

        // Merge updates
        ApplyRequestedValues(existing, request);
        existing.CategoryId = category.Value;
        existing.Version = request.Version;
        existing.RecordTypeName = request.RecordTypeName;
        if (existing is IFederatedDataSetConfiguration federated)
            federated.FederationStrategy = mergedFederationStrategy;
        ApplyRequestedChildren(existing, request);

        var saveResult = await _dataSetProvider.Update(existing, "DataSet", existing.Implementation, existing.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            DataSetEndpointLog.DataSetUpdateFailed(Logger, request.Name, "Save failed");
            return saveResult.ToNewResult<DataSetDetailResponse>();
        }

        DataSetEndpointLog.DataSetUpdated(Logger, request.Name);
        var detail = await Detail(existing, ct).ConfigureAwait(false);
        return detail.IsFailure || detail.Value is null ? detail.ToNewResult<DataSetDetailResponse>() : GenericResult<DataSetDetailResponse>.Success(detail.Value);
    }

    // A category left out leaves the stored one; an empty name takes the data set out of its category; a name must be a category of the
    // DataSet type.
    private async Task<IGenericResult<Guid?>> RequestedCategory(
        UpdateDataSetRequest request, IDataSetImplementationConfiguration existing, CancellationToken ct)
    {
        if (request.Category is null) return GenericResult<Guid?>.Success(existing.CategoryId);

        var tree = await DataSetCategoryLinks.Tree(_categories, Logger, ct).ConfigureAwait(false);
        return tree.IsFailure || tree.Value is null
            ? tree.ToNewResult<Guid?>()
            : DataSetCategoryLinks.Resolve(tree.Value, request.Category, Logger);
    }

    // The data set as the API answers it: its category by name and path.
    private async Task<IGenericResult<DataSetDetailResponse?>> Detail(IDataSetImplementationConfiguration dataSet, CancellationToken ct)
    {
        var tree = await DataSetCategoryLinks.Tree(_categories, Logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null) return tree.ToNewResult<DataSetDetailResponse?>();

        var described = DataSetCategoryLinks.Describe(DataSetQueryHelper.MapToDetail(dataSet), tree.Value, dataSet.CategoryId, Logger);
        return described.IsFailure
            ? described.ToNewResult<DataSetDetailResponse?>()
            : GenericResult<DataSetDetailResponse?>.Success(described.Value);
    }

    // Each kind is its own implementation (FDW-1031): a data set cannot become another kind, and carries only
    // what its kind has.
    private IGenericResult<bool> ValidateKind(
        UpdateDataSetRequest request, IDataSetImplementationConfiguration existing, string mergedImplementation)
    {
        if (!string.Equals(mergedImplementation, existing.Implementation, StringComparison.Ordinal))
        {
            return GenericResult<bool>.Failure(
                DataSetsResultCodes.ImplementationChangeNotAllowed, Logger,
                ResultDetails.Create("name", request.Name, "implementation", existing.Implementation, "requested", mergedImplementation));
        }

        return DataSetQueryHelper.ValidateKindParts(
            mergedImplementation, request.Joins is { Count: > 0 }, request.Aggregates is { Count: > 0 }, request.Name, Logger);
    }

    // The child lists the request carries, each validated only when present (a null list leaves
    // the stored one alone). The first failure is the answer.
    private IGenericResult<bool> ValidateRequestedChildren(UpdateDataSetRequest request)
    {
        // The data set's classification when the PATCH names one and, when fields are replaced, each field's.
        IEnumerable<int> fieldClassifications = request.Fields is null ? [] : request.Fields.Select(f => f.ClassificationId);
        var classified = DataClassificationValidator.Validate(
            request.ClassificationId is { } named ? fieldClassifications.Prepend(named) : fieldClassifications, Logger);
        if (classified.IsFailure) return classified.ToNewResult<bool>();

        // Each replaced field's datum type and data type, when fields are replaced.
        var typed = DataSetQueryHelper.ValidateFieldTypes(request.Fields, Logger);
        if (typed.IsFailure) return typed.ToNewResult<bool>();

        if (request.Aggregates is not null)
        {
            var aggregates = DataSetQueryHelper.ValidateAggregates(request.Aggregates, request.Name, Logger);
            if (aggregates.IsFailure) return aggregates;
        }

        return request.Sources is null
            ? GenericResult<bool>.Success(true)
            : DataSetQueryHelper.ValidateFieldMappings(request.Sources, request.Name, Logger);
    }

    // The values the request carries; a value left out leaves the stored one unchanged.
    private static void ApplyRequestedValues(IDataSetImplementationConfiguration existing, UpdateDataSetRequest request)
    {
        existing.Description = request.Description ?? existing.Description;
        if (request.ClassificationId is { } classificationId)
        {
            existing.ClassificationId = classificationId;
        }

        existing.SourceDataSetName = request.SourceDataSetName ?? existing.SourceDataSetName;
    }

    private static void ApplyRequestedChildren(IDataSetImplementationConfiguration existing, UpdateDataSetRequest request)
    {
            if (request.KeyFields is not null)
            {
                existing.KeyFields = request.KeyFields
                    .Select((name, i) => new DataSetKeyFieldConfiguration
                    {
                        KeyName = name,
                        KeyType = "Surrogate",
                        Ordinal = i
                    })
                    .ToList();
            }

            if (request.Filters is not null)
            {
                existing.Filters = request.Filters
                    .Select((f, i) => new DataSetFilterConditionConfiguration
                    {
                        FieldName = f.FieldName,
                        Operator = f.Operator,
                        Value = f.Value,
                        DataType = f.DataType,
                        Ordinal = i
                    })
                    .ToList();
            }

            if (request.Fields is not null)
            {
                existing.Fields = DataSetQueryHelper.MapFields(request.Fields);
            }

            if (request.Sources is not null)
            {
                existing.Sources = DataSetQueryHelper.MapSources(request.Sources, existing.Sources);
            }

            if (request.Joins is not null && existing is IJoinedDataSetConfiguration joined)
            {
                joined.Joins = DataSetQueryHelper.MapJoins(request.Joins);
            }

            if (request.Caching is not null)
            {
                existing.Caching = DataSetQueryHelper.MapCaching(request.Caching);
            }

            if (request.Aggregates is not null && existing is ISimpleDataSetConfiguration simple)
            {
                simple.Aggregates = DataSetQueryHelper.MapAggregates(request.Aggregates);
            }
    }

}
