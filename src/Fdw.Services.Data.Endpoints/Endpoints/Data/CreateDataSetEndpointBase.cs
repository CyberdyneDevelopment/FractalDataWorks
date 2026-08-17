using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new data set configuration.
/// Delegates all reads and writes to DataSetConfigurationProvider.
/// </summary>
public abstract class CreateDataSetEndpointBase : CrudCreateEndpoint<CreateDataSetRequest, DataSetDetailResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected CreateDataSetEndpointBase(DataSetConfigurationProvider dataSetProvider)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <summary>Returns the data set name from the create request.</summary>
    protected override string GetResourceName(CreateDataSetRequest request) => request.Name;

    /// <summary>Checks whether a data set with the requested name already exists.</summary>
    protected override Task<IGenericResult<bool>> CheckExists(CreateDataSetRequest request, CancellationToken ct)
    {
        return CheckDataSetExists(request.Name, ct);
    }

    /// <summary>Creates the data set configuration and persists it.</summary>
    protected override Task<IGenericResult<DataSetDetailResponse>> Create(CreateDataSetRequest request, CancellationToken ct)
    {
        return CreateDataSet(request, ct);
    }

    /// <summary>
    /// Checks if a data set already exists by querying the provider.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<bool>> CheckDataSetExists(string name, CancellationToken ct)
    {
        var result = await _dataSetProvider.Get(name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<bool>();

        var exists = result.Value is not null;
        if (exists) DataSetEndpointLog.DataSetAlreadyExists(Logger, name);
        return GenericResult<bool>.Success(exists);
    }

    /// <summary>
    /// Creates and saves a new data set, returning as detail DTO.
    /// Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<DataSetDetailResponse>> CreateDataSet(CreateDataSetRequest request, CancellationToken ct)
    {
        DataSetEndpointLog.CreatingDataSet(Logger, request.Name);

        // Why: the strategy discriminator is authored, never defaulted — a dataset whose
        // ServiceOptionType is not a registered DataSetTypes member (Simple/Compound/Federated) could
        // not be dispatched at execution time. Fail loud at create rather than persist a dead dataset.
        if (string.IsNullOrWhiteSpace(request.ServiceOptionType)
            || ReferenceEquals(DataSetTypes.ByName(request.ServiceOptionType), DataSetTypes.NotFound))
        {
            return GenericResult<DataSetDetailResponse>.Failure(
                DataSetsResultCodes.ServiceOptionTypeInvalid, Logger,
                ResultDetails.Create("name", request.Name, "serviceOptionType", request.ServiceOptionType ?? string.Empty));
        }

        var federationValidation = DataSetQueryHelper.ValidateFederationStrategy(
            request.ServiceOptionType, request.FederationStrategy, request.Name, Logger);
        if (federationValidation.IsFailure) return federationValidation.ToNewResult<DataSetDetailResponse>();

        var aggregatesValidation = DataSetQueryHelper.ValidateAggregates(request.Aggregates, request.Name, Logger);
        if (aggregatesValidation.IsFailure) return aggregatesValidation.ToNewResult<DataSetDetailResponse>();

        var config = new DataSetConfiguration
        {
            // Why: Id is Guid.Empty — DefaultConfigurationProvider.Save mints UUIDv7 on insert.
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Category = request.Category ?? "Dataset",
            Version = request.Version,
            RecordTypeName = request.RecordTypeName,
            KeyFields = request.KeyFields
                .Select((name, i) => new DataSetKeyFieldConfiguration
                {
                    KeyName = name,
                    KeyType = "Surrogate",
                    Ordinal = i
                })
                .ToList(),
            // Why: validated above to be a registered DataSetTypes member — bound off the request,
            // never defaulted.
            ServiceOptionType = request.ServiceOptionType,
            // Why: validated above (Federated requires a registered FederationStrategies member; every
            // other strategy must leave it null) — bound off the request, never defaulted.
            FederationStrategy = request.FederationStrategy,
            TransformExpression = request.TransformExpression,
            SourceDataSetName = request.SourceDataSetName,
            Fields = DataSetQueryHelper.MapFields(request.Fields),
            // Nothing exists yet on create, so no source has a prior identity to keep.
            Sources = DataSetQueryHelper.MapSources(request.Sources, []),
            Joins = DataSetQueryHelper.MapJoins(request.Joins),
            Caching = DataSetQueryHelper.MapCaching(request.Caching),
            // Why: validated above (each function name resolves against AggregationFunctions; every
            // groupByFieldNames splits into non-empty elements) — bound off the request, never defaulted.
            Aggregates = DataSetQueryHelper.MapAggregates(request.Aggregates)
        };

        var saveResult = await _dataSetProvider.Save(config, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            DataSetEndpointLog.DataSetCreateFailed(Logger, request.Name, "Save failed");
            return saveResult.ToNewResult<DataSetDetailResponse>();
        }

        DataSetEndpointLog.DataSetCreated(Logger, config.Name);
        return GenericResult<DataSetDetailResponse>.Success(DataSetQueryHelper.MapToDetail(config));
    }

    /// <summary>Sends a 201 Created response with the data set detail.</summary>
    protected override Task SendCreatedResponse(DataSetDetailResponse detail, CancellationToken ct)
    {
        return Send.ResponseAsync(detail, 201, ct);
    }
}
