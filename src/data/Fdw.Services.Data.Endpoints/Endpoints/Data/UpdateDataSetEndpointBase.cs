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
    private readonly IDataStoreConfigurationProvider _dataStores;

    /// <summary>Initializes a new instance of the <see cref="UpdateDataSetEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and writes the data set.</param>
    /// <param name="categories">Reads the category tree a category name is resolved in.</param>
    /// <param name="dataStores">Reads the data stores the sources name, from which the data set's kind is worked out (FDW-6806).</param>
    protected UpdateDataSetEndpointBase(
        ILogger<UpdateDataSetEndpointBase> logger,
        IDataSetConfigurationProvider dataSetProvider,
        ICategoryConfigurationProvider categories,
        IDataStoreConfigurationProvider dataStores) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        _categories = categories;
        _dataStores = dataStores;
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

        var childrenValidation = ValidateRequestedChildren(request);
        if (childrenValidation.IsFailure) return childrenValidation.ToNewResult<DataSetDetailResponse>();

        var category = await RequestedCategory(request, existing, ct).ConfigureAwait(false);
        if (category.IsFailure) return category.ToNewResult<DataSetDetailResponse>();

        // Merge updates
        ApplyRequestedValues(existing, request);
        existing.CategoryId = category.Value;
        existing.Version = request.Version ?? existing.Version;
        existing.RecordTypeName = request.RecordTypeName;
        var children = ApplyRequestedChildren(existing, request, new DataSetRequestValues(request.Name, Logger));
        if (children.IsFailure) return children.ToNewResult<DataSetDetailResponse>();

        // The kind follows the merged sources (FDW-6806): a data set whose sources now make another kind moves to it, carrying everything.
        var kinded = await Kinded(existing, request, ct).ConfigureAwait(false);
        if (kinded.IsFailure) return kinded.ToNewResult<DataSetDetailResponse>();

        var updated = kinded.Value!;

        // Checked over the merged set, so a source whose mappings the request leaves out is checked with its stored ones (FDW-5022).
        if (DataSetQueryHelper.ValidateSaved(updated, Logger) is { IsFailure: true } refused)
            return refused.ToNewResult<DataSetDetailResponse>();

        var saveResult = await _dataSetProvider.Update(updated, "DataSet", updated.Implementation, updated.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            DataSetEndpointLog.DataSetUpdateFailed(Logger, request.Name, "Save failed");
            return saveResult.ToNewResult<DataSetDetailResponse>();
        }

        DataSetEndpointLog.DataSetUpdated(Logger, request.Name);
        var detail = await Detail(updated, ct).ConfigureAwait(false);
        return detail.IsFailure || detail.Value is null ? detail.ToNewResult<DataSetDetailResponse>() : GenericResult<DataSetDetailResponse>.Success(detail.Value);
    }

    // The merged data set as the kind its sources make (FDW-6806): the same configuration when the kind is unchanged, else the configuration of
    // the new kind carrying everything (DataSetKindMove), and in both the request's joins, statement of how the sources combine and federation
    // strategy, which only the joined kinds have. Refused when the kind cannot carry a part (aggregates on a joined kind, joins on Simple), and for a Graph data
// set or a Variation, whose kind its sources do not make (ImplementationChangeNotAllowed).
    [ApprovedFallback("an update that omits a field keeps the stored value")]
    private async Task<IGenericResult<IDataSetImplementationConfiguration>> Kinded(
        IDataSetImplementationConfiguration existing, UpdateDataSetRequest request, CancellationToken ct)
    {
        var kind = await DataSetKindDerivation.Derive(
            request.Name, [.. existing.Sources.Select(source => (source.SourceName, (string?)source.DataStoreName, (string?)source.SourceKind))], _dataStores, Logger, ct)
            .ConfigureAwait(false);
        if (kind.IsFailure) return kind.ToNewResult<IDataSetImplementationConfiguration>();

        // A Graph data set and a Variation have no sources to make their kind, so the kind the sources make is never theirs: this route would move one to Simple and drop
        // what Simple has no place for. They are updated by their own routes (FDW-6129).
        if (!DataSetKindDerivation.FollowsItsSources(existing.Implementation))
            return GenericResult<IDataSetImplementationConfiguration>.Failure(
                DataSetsResultCodes.ImplementationChangeNotAllowed, Logger,
                ResultDetails.Create("name", request.Name, "implementation", existing.Implementation, "requested", kind.Value!));

        var parts = DataSetQueryHelper.ValidateKindParts(
            kind.Value!, request.Joins is { Count: > 0 }, request.Aggregates is { Count: > 0 }, request.Name, Logger);
        if (parts.IsFailure) return parts.ToNewResult<IDataSetImplementationConfiguration>();

        var mergedFederationStrategy = request.FederationStrategy ?? existing.FederationStrategyOrNone();
        var strategy = DataSetQueryHelper.ValidateFederationStrategy(kind.Value!, mergedFederationStrategy, request.Name, Logger);
        if (strategy.IsFailure) return strategy.ToNewResult<IDataSetImplementationConfiguration>();

        var target = string.Equals(kind.Value, existing.Implementation, StringComparison.Ordinal)
            ? GenericResult<IDataSetImplementationConfiguration>.Success(existing)
            : DataSetKindMove.To(existing, kind.Value!, Logger);
        if (target.IsFailure) return target;

        ApplyKindParts(target.Value!, request, mergedFederationStrategy);
        return target;
    }

    // The parts only some kinds have, on the data set as the kind its sources make: a Simple one's aggregates, a joined one's joins and statement
    // of how the sources combine (FDW-6946), each replacing the stored ones when sent, and a Federated one's merged federation strategy.
    private static void ApplyKindParts(IDataSetImplementationConfiguration dataSet, UpdateDataSetRequest request, string? federationStrategy)
    {
        if (request.Aggregates is not null && dataSet is ISimpleDataSetConfiguration simple)
            simple.Aggregates = DataSetQueryHelper.MapAggregates(request.Aggregates);

        if (dataSet is not IJoinedDataSetConfiguration joined)
            return;

        if (request.Joins is not null)
            joined.Joins = DataSetQueryHelper.MapJoins(request.Joins);

        if (request.SourceCombination is { } combination)
            joined.SourceCombination = combination;

        if (dataSet is IFederatedDataSetConfiguration federated)
        {
            federated.FederationStrategy = federationStrategy;
            if (request.FederationOptions is not null)
                federated.FederationOptions = DataSetQueryHelper.MapFederationOptions(request.FederationOptions);
        }
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

    // The child lists the request carries, each validated only when present (a null list leaves
    // the stored one alone). The first failure is the answer.
    private IGenericResult<bool> ValidateRequestedChildren(UpdateDataSetRequest request)
    {
        // The display name, before anything is written (FDW-6117).
        var displayNameChecked = DataSetNames.Check(request.DisplayName, request.Abbreviation, request.Name, Logger);
        if (displayNameChecked.IsFailure) return displayNameChecked;

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
        DataSetNames.Set(existing, request.DisplayName, request.Abbreviation);

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
                existing.Filters = DataSetQueryHelper.MapFilters(request.Filters);
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
