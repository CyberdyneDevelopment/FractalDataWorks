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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for creating a new data set configuration.
/// Delegates all reads and writes to DataSetConfigurationProvider.
/// </summary>
public abstract class CreateDataSetEndpointBase : CrudCreateEndpointBase<CreateDataSetRequest, DataSetDetailResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected CreateDataSetEndpointBase(ILogger<CreateDataSetEndpointBase> logger, DataSetConfigurationProvider dataSetProvider) : base(logger)
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

        if (string.IsNullOrWhiteSpace(request.Implementation)
            || ReferenceEquals(DataSetTypes.ByName(request.Implementation), DataSetTypes.NotFound))
        {
            return GenericResult<DataSetDetailResponse>.Failure(
                DataSetsResultCodes.ImplementationInvalid, Logger,
                ImplementationInvalidCode.Describe(request.Name, request.Implementation));
        }

        var federationValidation = DataSetQueryHelper.ValidateFederationStrategy(
            request.Implementation, request.FederationStrategy, request.Name, Logger);
        if (federationValidation.IsFailure) return federationValidation.ToNewResult<DataSetDetailResponse>();

        var partsValidation = DataSetQueryHelper.ValidateKindParts(
            request.Implementation, request.Joins.Count > 0, request.Aggregates.Count > 0, request.Name, Logger);
        if (partsValidation.IsFailure) return partsValidation.ToNewResult<DataSetDetailResponse>();

        var aggregatesValidation = DataSetQueryHelper.ValidateAggregates(request.Aggregates, request.Name, Logger);
        if (aggregatesValidation.IsFailure) return aggregatesValidation.ToNewResult<DataSetDetailResponse>();

        var mappingsValidation = DataSetQueryHelper.ValidateFieldMappings(request.Sources, request.Name, Logger);
        if (mappingsValidation.IsFailure) return mappingsValidation.ToNewResult<DataSetDetailResponse>();

        // The data set's classification and each field's, before anything is written.
        var classified = DataClassificationValidator.Validate(request.Fields.Select(f => f.ClassificationId).Prepend(request.ClassificationId), Logger);
        if (classified.IsFailure) return classified.ToNewResult<DataSetDetailResponse>();

        // Each field's datum type and data type.
        var typed = DataSetQueryHelper.ValidateFieldTypes(request.Fields, Logger);
        if (typed.IsFailure) return typed.ToNewResult<DataSetDetailResponse>();

        var built = DataSetConfigurationFactory.Create(request, Logger);
        if (built.IsFailure) return built.ToNewResult<DataSetDetailResponse>();

        var config = built.Value!;
        var saveResult = await _dataSetProvider.Save(config, "DataSet", config.Implementation, config.Name, ct).ConfigureAwait(false);
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
