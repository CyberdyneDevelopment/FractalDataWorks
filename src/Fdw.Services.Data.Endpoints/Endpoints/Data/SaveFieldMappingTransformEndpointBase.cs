using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.DataSets;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for creating or updating a field mapping transform step.
/// Provides virtual default implementations using IDataGateway for reads and writes.
/// </summary>
/// <remarks>
/// Why: FieldMappingTransform and FieldMappingTransformParameter are child records that do not
/// implement IGenericConfiguration, so they cannot be routed through IConfigurationWriter.
/// ConfigurationSaveCommand via IDataGateway is the documented exception for child records.
/// The configuration connection name and path are sourced from DataSetConfigurationProvider
/// rather than IConfigurationConnectionNameProvider (which is an anti-pattern).
/// </remarks>
public abstract class SaveFieldMappingTransformEndpointBase : CrudCreateEndpoint<SaveFieldMappingTransformRequest, FieldMappingTransformPayload>
{
    /// <summary>Gets the data gateway for executing queries and commands.</summary>
    protected IDataGateway DataGateway { get; }

    // Why: DataSetConfigurationProvider owns the DataStoreName and PathName for the
    // configuration database — this is the single source of truth rather than
    // IConfigurationConnectionNameProvider (which is the anti-pattern being eliminated).
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected SaveFieldMappingTransformEndpointBase(
        IDataGateway dataGateway,
        DataSetConfigurationProvider dataSetProvider)
    {
        DataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "field-mapping-transforms";

    /// <summary>Gets the write policy for this endpoint.</summary>
    protected override string WritePolicy => "datasets:write";

    /// <summary>Gets the route for this endpoint.</summary>
    protected override string Route => "/field-mappings/{FieldMappingId}/transforms";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "Save a field mapping transform";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Creates or updates a transform step in a field mapping's transform chain.";

    /// <summary>Gets the connection name for configuration database queries.</summary>
    protected virtual string ConfigurationConnectionName => _dataSetProvider.DataStoreName;

    /// <summary>
    /// Gets the configuration path that holds the transform containers.
    /// </summary>
    // Why not the dataset provider's PathName: these containers are declared in ConfigurationDb's
    // "transform" path, not "data". Borrowing the dataset path made every addressed lookup fail with
    // "DataContainer 'FieldMappingTransform' not found in path 'data'", which surfaced as a 404 on
    // every transform route. The container names beside this are literals for the same reason — the
    // endpoint names the location it targets rather than inheriting one that happens to differ.
    protected virtual string TransformPathName => "transform";

    /// <summary>Gets the container name for FieldMappingTransform queries.</summary>
    protected virtual string TransformContainerName => "FieldMappingTransform";

    /// <summary>Gets the container name for FieldMappingTransformParameter queries.</summary>
    protected virtual string TransformParameterContainerName => "FieldMappingTransformParameter";

    /// <summary>Returns the transform type from the save request.</summary>
    protected override string GetResourceName(SaveFieldMappingTransformRequest request) => request.TransformType;

    /// <summary>
    /// Checks whether a transform already exists. Always returns false since transforms
    /// are identified by position, not uniqueness constraints.
    /// </summary>
    protected override Task<IGenericResult<bool>> CheckExists(SaveFieldMappingTransformRequest request, CancellationToken ct)
    {
        return Task.FromResult(GenericResult<bool>.Success(false));
    }

    /// <summary>Creates or updates the transform and its parameters.</summary>
    protected override Task<IGenericResult<FieldMappingTransformPayload>> Create(SaveFieldMappingTransformRequest request, CancellationToken ct)
    {
        return SaveTransform(request, ct);
    }

    /// <summary>
    /// Saves a field mapping transform and its parameters.
    /// Default implementation uses IDataGateway with ConfigurationSaveCommand. Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<FieldMappingTransformPayload>> SaveTransform(SaveFieldMappingTransformRequest request, CancellationToken ct)
    {
        FieldMappingTransformEndpointLog.SavingTransform(Logger, request.TransformType);

        var config = new FieldMappingTransformConfiguration
        {
            // Why: Id = default (Guid.Empty) signals INSERT with a new UUIDv7.
            // The provider/gateway mints the ID; no Guid.NewGuid() here.
            Id = default,
            DataSetFieldMappingId = request.FieldMappingId,
            TransformType = request.TransformType,
            Ordinal = request.Ordinal
        };

        // Why: Addressing moved off IDataCommand onto DataStoreTarget.
        var saveResult = await DataGateway
            .Execute<int>(
                new ConfigurationSaveCommand<FieldMappingTransformConfiguration>(config),
                new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformContainerName),
                ct)
            .ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            FieldMappingTransformEndpointLog.SaveTransformFailed(Logger, request.TransformType);
            return saveResult.ToNewResult<FieldMappingTransformPayload>();
        }

        // Save parameters
        var savedParams = new List<FieldMappingTransformParameterPayload>();
        foreach (var param in request.Parameters)
        {
            var paramConfig = new FieldMappingTransformParameterConfiguration
            {
                // Why: Id = default signals INSERT with a new UUIDv7 from the gateway.
                Id = default,
                FieldMappingTransformId = config.Id,
                Name = param.Name,
                Value = param.Value
            };

            var paramResult = await DataGateway
                .Execute<int>(
                    new ConfigurationSaveCommand<FieldMappingTransformParameterConfiguration>(paramConfig),
                    new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformParameterContainerName),
                    ct)
                .ConfigureAwait(false);
            if (paramResult.IsFailure)
            {
                FieldMappingTransformEndpointLog.SaveTransformFailed(Logger, request.TransformType);
                return paramResult.ToNewResult<FieldMappingTransformPayload>();
            }

            savedParams.Add(new FieldMappingTransformParameterPayload
            {
                Id = paramConfig.Id,
                TransformId = config.Id,
                Name = paramConfig.Name,
                Value = paramConfig.Value
            });
        }

        FieldMappingTransformEndpointLog.SavedTransform(Logger, request.TransformType);

        return GenericResult<FieldMappingTransformPayload>.Success(new FieldMappingTransformPayload
        {
            Id = config.Id,
            FieldMappingId = config.DataSetFieldMappingId,
            TransformType = config.TransformType,
            Ordinal = config.Ordinal,
            Parameters = savedParams
        });
    }
}
