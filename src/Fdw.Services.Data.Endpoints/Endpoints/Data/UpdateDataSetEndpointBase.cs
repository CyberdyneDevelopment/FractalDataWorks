using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    /// <inheritdoc />
    protected UpdateDataSetEndpointBase(DataSetConfigurationProvider dataSetProvider)
    {
        _dataSetProvider = dataSetProvider;
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

        return GenericResult<DataSetDetailResponse?>.Success((DataSetDetailResponse?)DataSetQueryHelper.MapToDetail(result.Value));
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

        var mergedServiceOptionType = request.ServiceOptionType ?? existing.ServiceOptionType;
        if (string.IsNullOrWhiteSpace(mergedServiceOptionType)
            || ReferenceEquals(DataSetTypes.ByName(mergedServiceOptionType), DataSetTypes.NotFound))
        {
            return GenericResult<DataSetDetailResponse>.Failure(
                DataSetsResultCodes.ServiceOptionTypeInvalid, Logger,
                ResultDetails.Create("name", request.Name, "serviceOptionType", mergedServiceOptionType ?? string.Empty));
        }

        var mergedFederationStrategy = request.FederationStrategy ?? existing.FederationStrategy;
        var federationValidation = DataSetQueryHelper.ValidateFederationStrategy(
            mergedServiceOptionType, mergedFederationStrategy, request.Name, Logger);
        if (federationValidation.IsFailure) return federationValidation.ToNewResult<DataSetDetailResponse>();

        var aggregatesValidation = ValidateRequestedAggregates(request);
        if (aggregatesValidation.IsFailure) return aggregatesValidation.ToNewResult<DataSetDetailResponse>();

        // Merge updates
        existing.Description = request.Description ?? existing.Description;
        existing.Category = request.Category ?? existing.Category;
        existing.Version = request.Version;
        existing.RecordTypeName = request.RecordTypeName;
        existing.ServiceOptionType = mergedServiceOptionType;
        existing.FederationStrategy = mergedFederationStrategy;
        existing.TransformExpression = request.TransformExpression ?? existing.TransformExpression;
        existing.SourceDataSetName = request.SourceDataSetName ?? existing.SourceDataSetName;
        ApplyRequestedChildren(existing, request);

        var saveResult = await _dataSetProvider.Save(existing, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            DataSetEndpointLog.DataSetUpdateFailed(Logger, request.Name, "Save failed");
            return saveResult.ToNewResult<DataSetDetailResponse>();
        }

        DataSetEndpointLog.DataSetUpdated(Logger, request.Name);
        return GenericResult<DataSetDetailResponse>.Success(DataSetQueryHelper.MapToDetail(existing));
    }

    private IGenericResult<bool> ValidateRequestedAggregates(UpdateDataSetRequest request)
        => request.Aggregates is null
            ? GenericResult<bool>.Success(true)
            : DataSetQueryHelper.ValidateAggregates(request.Aggregates, request.Name, Logger);

    private static void ApplyRequestedChildren(DataSetConfiguration existing, UpdateDataSetRequest request)
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

            if (request.Joins is not null)
            {
                existing.Joins = DataSetQueryHelper.MapJoins(request.Joins);
            }

            if (request.Caching is not null)
            {
                existing.Caching = DataSetQueryHelper.MapCaching(request.Caching);
            }

            if (request.Aggregates is not null)
            {
                existing.Aggregates = DataSetQueryHelper.MapAggregates(request.Aggregates);
            }
    }

}
