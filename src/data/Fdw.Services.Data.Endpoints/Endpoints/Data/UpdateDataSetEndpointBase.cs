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
using Fdw.Conventions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for updating an existing data set configuration.
/// Delegates all reads and writes to IDataSetConfigurationProvider.
/// </summary>
public abstract class UpdateDataSetEndpointBase : CrudUpdateEndpointBase<UpdateDataSetRequest, DataSetDetailResponse>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly ICategoryConfigurationProvider _categories;

    /// <inheritdoc />
    protected UpdateDataSetEndpointBase(
        ILogger<UpdateDataSetEndpointBase> logger, IDataSetConfigurationProvider dataSetProvider, ICategoryConfigurationProvider categories) : base(logger)
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
    [ApprovedFallback("an update that omits a field keeps the stored value")]
    protected virtual async Task<IGenericResult<DataSetDetailResponse>> SaveUpdatedDataSet(UpdateDataSetRequest request, CancellationToken ct)
    {
        DataSetEndpointLog.UpdatingDataSet(Logger, request.Name);

        var loadResult = await _dataSetProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (loadResult.IsFailure) return loadResult.ToNewResult<DataSetDetailResponse>();

        if (loadResult.Value is null)
            return GenericResult<DataSetDetailResponse>.Failure(DataSetEndpointLog.DataSetNotFound(Logger, request.Name));

        var existing = loadResult.Value;

        var merged = ValidateMerge(request, existing);
        if (merged.IsFailure) return merged.ToNewResult<DataSetDetailResponse>();

        var mergedFederationStrategy = merged.Value;

        var category = await RequestedCategory(request, existing, ct).ConfigureAwait(false);
        if (category.IsFailure) return category.ToNewResult<DataSetDetailResponse>();

        // Merge updates
        ApplyRequestedValues(existing, request);
        existing.CategoryId = category.Value;
        existing.Version = request.Version ?? existing.Version;
        existing.RecordTypeName = request.RecordTypeName;
        if (existing is IFederatedDataSetConfiguration federated)
            federated.FederationStrategy = mergedFederationStrategy;
        var children = ApplyRequestedChildren(existing, request, new DataSetRequestValues(request.Name, Logger));
        if (children.IsFailure) return children.ToNewResult<DataSetDetailResponse>();

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

    // The request merged over the stored data set, refused when its implementation, kind, federation strategy or children are not valid;
    // the merged federation strategy, which may be none. Split from SaveUpdatedDataSet for FDW007 (complexity limit 15).
    [ApprovedFallback("an update that omits a field keeps the stored value")]
    private IGenericResult<string?> ValidateMerge(UpdateDataSetRequest request, IDataSetImplementationConfiguration existing)
    {
        var mergedImplementation = request.Implementation ?? existing.Implementation;
        if (string.IsNullOrWhiteSpace(mergedImplementation)
            || ReferenceEquals(DataSetTypes.ByName(mergedImplementation), DataSetTypes.NotFound))
        {
            return GenericResult<string?>.Failure(
                DataSetsResultCodes.ImplementationInvalid, Logger,
                ImplementationInvalidCode.Describe(request.Name, mergedImplementation));
        }

        var kindValidation = ValidateKind(request, existing, mergedImplementation);
        if (kindValidation.IsFailure) return kindValidation.ToNewResult<string?>();

        var mergedFederationStrategy = request.FederationStrategy ?? existing.FederationStrategyOrNone();
        var federationValidation = DataSetQueryHelper.ValidateFederationStrategy(
            mergedImplementation, mergedFederationStrategy, request.Name, Logger);
        if (federationValidation.IsFailure) return federationValidation.ToNewResult<string?>();

        var childrenValidation = ValidateRequestedChildren(request);
        return childrenValidation.IsFailure
            ? childrenValidation.ToNewResult<string?>()
            : GenericResult<string?>.Success(mergedFederationStrategy);
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

        if (request.Filters is not null)
        {
            var filters = DataSetQueryHelper.ValidateFilters(request.Filters, request.Name, Logger);
            if (filters.IsFailure) return filters;
        }

        return request.Sources is null
            ? GenericResult<bool>.Success(true)
            : DataSetQueryHelper.ValidateFieldMappings(request.Sources, request.Name, Logger);
    }

    // Sets each value the request sends; a field the request does not send is not touched.
    private static void ApplyRequestedValues(IDataSetImplementationConfiguration existing, UpdateDataSetRequest request)
    {
        if (request.Description is { } description)
        {
            existing.Description = description;
        }

        if (request.ClassificationId is { } classificationId)
        {
            existing.ClassificationId = classificationId;
        }

        if (request.SourceDataSetName is { } sourceDataSetName)
        {
            existing.SourceDataSetName = sourceDataSetName;
        }
    }

    private static IGenericResult ApplyRequestedChildren(
        IDataSetImplementationConfiguration existing, UpdateDataSetRequest request, DataSetRequestValues values)
    {
            var replaced = ReplaceFieldsAndSources(existing, request, values);
            if (replaced.IsFailure) return replaced;

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

            if (request.Joins is not null && existing is IJoinedDataSetConfiguration joined)
            {
                joined.Joins = DataSetQueryHelper.MapJoins(request.Joins);
            }

            if (request.Caching is not null)
            {
                var caching = DataSetQueryHelper.MapCaching(request.Caching, existing.Implementation, values);
                if (caching.IsFailure) return caching;
                existing.Caching = caching.Value;
            }

            if (request.Aggregates is not null && existing is ISimpleDataSetConfiguration simple)
            {
                simple.Aggregates = DataSetQueryHelper.MapAggregates(request.Aggregates);
            }

            return GenericResult.Success();
    }

    // The fields and sources the request replaces. One the data set already has keeps what the request does not state; a new one must
    // state the values its caller chooses, or the update is refused (FDW-3931).
    private static IGenericResult ReplaceFieldsAndSources(
        IDataSetImplementationConfiguration existing, UpdateDataSetRequest request, DataSetRequestValues values)
    {
        if (request.Fields is not null)
        {
            var fields = DataSetQueryHelper.MapFields(request.Fields, existing.Fields, values);
            if (fields.IsFailure) return fields;
            existing.Fields = fields.Value!;
        }

        if (request.Sources is not null)
        {
            var sources = DataSetQueryHelper.MapSources(request.Sources, existing.Sources, values);
            if (sources.IsFailure) return sources;
            existing.Sources = sources.Value!;
        }

        return GenericResult.Success();
    }

}
