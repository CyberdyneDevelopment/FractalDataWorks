using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.DataSets;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Changes a transform already in a field mapping's chain.</summary>
/// <remarks>
/// Why this exists: the save path builds its configuration with <c>Id = default</c>, which the
/// gateway reads as "insert and mint an id", and the request it takes names the mapping but never
/// the transform. So a transform could be added, reordered and deleted but never edited — changing
/// one meant deleting and re-adding it, which drops it to the end of the chain.
///
/// Parameters are replaced rather than merged: a transform's parameters are its configuration, and
/// a caller sending a shorter list means those parameters are gone, not unchanged.
/// </remarks>
public abstract class UpdateFieldMappingTransformEndpointBase
    : CrudUpdateEndpointBase<UpdateFieldMappingTransformRequest, FieldMappingTransformPayload>
{
    /// <summary>Gets the gateway every read and write goes through.</summary>
    protected IDataGateway DataGateway { get; }

    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="UpdateFieldMappingTransformEndpointBase"/> class.</summary>
    /// <param name="dataGateway">The gateway used for all reads and writes.</param>
    /// <param name="dataSetProvider">Owns the configuration store's name and path.</param>
    protected UpdateFieldMappingTransformEndpointBase(
        IDataGateway dataGateway,
        DataSetConfigurationProvider dataSetProvider)
    {
        DataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "field-mapping-transforms";

    /// <inheritdoc />
    protected override string WritePolicy => "datasets:write";

    /// <inheritdoc />
    protected override string Route => "/field-mappings/{FieldMappingId}/transforms/{TransformId}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Update a field mapping transform";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Changes a transform's type, its position in the chain, and the parameters it is applied with.";

    /// <summary>Gets the connection name for configuration database queries.</summary>
    protected virtual string ConfigurationConnectionName => _dataSetProvider.DataStoreName;

    /// <summary>Gets the configuration path that holds the transform containers.</summary>
    // Why not the dataset provider's PathName: these containers are declared in ConfigurationDb's
    // "transform" path, not "data". Borrowing the dataset path made every addressed lookup fail with
    // "DataContainer 'FieldMappingTransform' not found in path 'data'", which surfaced as a 404 on
    // every transform route.
    protected virtual string TransformPathName => "transform";

    /// <summary>Gets the container name for FieldMappingTransform queries.</summary>
    protected virtual string TransformContainerName => "FieldMappingTransform";

    /// <summary>Gets the container name for FieldMappingTransformParameter queries.</summary>
    protected virtual string TransformParameterContainerName => "FieldMappingTransformParameter";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UpdateFieldMappingTransformRequest request)
        => request.TransformId.ToString();

    /// <inheritdoc />
    protected override async Task<IGenericResult<FieldMappingTransformPayload?>> FindForUpdate(
        UpdateFieldMappingTransformRequest request,
        CancellationToken ct)
    {
        var existing = await LoadTransform(request.TransformId, ct).ConfigureAwait(false);
        if (existing.IsFailure)
        {
            return existing.ToNewResult<FieldMappingTransformPayload?>();
        }

        return GenericResult<FieldMappingTransformPayload?>.Success(existing.Value);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<FieldMappingTransformPayload>> Update(
        UpdateFieldMappingTransformRequest request,
        FieldMappingTransformPayload existing,
        CancellationToken ct)
        => UpdateTransform(request, ct);

    /// <summary>Applies the change to the transform row and replaces its parameters.</summary>
    /// <param name="request">The change being applied.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The transform as it stands after the change.</returns>
    protected virtual async Task<IGenericResult<FieldMappingTransformPayload>> UpdateTransform(
        UpdateFieldMappingTransformRequest request,
        CancellationToken ct)
    {
        FieldMappingTransformEndpointLog.UpdatingTransform(Logger, request.TransformId);

        // Why the id is carried rather than defaulted: default means insert to the gateway, which is
        // exactly why the save path could never edit.
        var config = new FieldMappingTransformConfiguration
        {
            Id = request.TransformId,
            DataSetFieldMappingId = request.FieldMappingId,
            TransformType = request.TransformType,
            Ordinal = request.Ordinal
        };

        var saveResult = await DataGateway
            .Execute<int>(
                new ConfigurationSaveCommand<FieldMappingTransformConfiguration>(config),
                new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformContainerName),
                ct)
            .ConfigureAwait(false);

        if (saveResult.IsFailure)
        {
            FieldMappingTransformEndpointLog.UpdateTransformFailed(Logger, request.TransformId);
            return saveResult.ToNewResult<FieldMappingTransformPayload>();
        }

        var parameters = await ReplaceParameters(request, ct).ConfigureAwait(false);
        if (parameters.IsFailure)
        {
            FieldMappingTransformEndpointLog.UpdateTransformFailed(Logger, request.TransformId);
            return parameters.ToNewResult<FieldMappingTransformPayload>();
        }

        FieldMappingTransformEndpointLog.UpdatedTransform(Logger, request.TransformId, request.TransformType);

        return GenericResult<FieldMappingTransformPayload>.Success(new FieldMappingTransformPayload
        {
            Id = request.TransformId,
            FieldMappingId = request.FieldMappingId,
            TransformType = request.TransformType,
            Ordinal = request.Ordinal,
            Parameters = parameters.Value ?? []
        });
    }

    /// <summary>Removes the transform's current parameters and writes the ones supplied.</summary>
    /// <param name="request">The change being applied.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The parameters as they stand after the change.</returns>
    protected virtual async Task<IGenericResult<IReadOnlyList<FieldMappingTransformParameterPayload>>> ReplaceParameters(
        UpdateFieldMappingTransformRequest request,
        CancellationToken ct)
    {
        var target = new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformParameterContainerName);

        var existingResult = await DataGateway
            .Execute<IEnumerable<FieldMappingTransformParameterConfiguration>>(
                new QueryCommand<FieldMappingTransformParameterConfiguration>
                {
                    Filter = DataSetQueryHelper.ActiveFilterFor(
                        nameof(FieldMappingTransformParameterConfiguration.FieldMappingTransformId), request.TransformId)
                },
                target,
                ct)
            .ConfigureAwait(false);

        if (existingResult.IsFailure)
        {
            return existingResult.ToNewResult<IReadOnlyList<FieldMappingTransformParameterPayload>>();
        }

        foreach (var stale in existingResult.Value ?? [])
        {
            var removed = await DataGateway
                .Execute<int>(new ConfigurationDeleteCommand(stale.Id), target, ct)
                .ConfigureAwait(false);

            if (removed.IsFailure)
            {
                return removed.ToNewResult<IReadOnlyList<FieldMappingTransformParameterPayload>>();
            }
        }

        var written = new List<FieldMappingTransformParameterPayload>();
        foreach (var param in request.Parameters)
        {
            var paramConfig = new FieldMappingTransformParameterConfiguration
            {
                // Why: default signals insert with a new UUIDv7 from the gateway.
                Id = default,
                FieldMappingTransformId = request.TransformId,
                Name = param.Name,
                Value = param.Value
            };

            var paramResult = await DataGateway
                .Execute<int>(
                    new ConfigurationSaveCommand<FieldMappingTransformParameterConfiguration>(paramConfig),
                    target,
                    ct)
                .ConfigureAwait(false);

            if (paramResult.IsFailure)
            {
                return paramResult.ToNewResult<IReadOnlyList<FieldMappingTransformParameterPayload>>();
            }

            written.Add(new FieldMappingTransformParameterPayload
            {
                Id = paramConfig.Id,
                Name = param.Name,
                Value = param.Value
            });
        }

        return GenericResult<IReadOnlyList<FieldMappingTransformParameterPayload>>.Success(written);
    }

    /// <summary>Reads the transform being changed so the caller learns it is missing before any write.</summary>
    /// <param name="transformId">The transform to read.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The transform, or null when no active transform carries that id.</returns>
    protected virtual async Task<IGenericResult<FieldMappingTransformPayload?>> LoadTransform(
        Guid transformId,
        CancellationToken ct)
    {
        var result = await DataGateway
            .Execute<IEnumerable<FieldMappingTransformConfiguration>>(
                new QueryCommand<FieldMappingTransformConfiguration>
                {
                    Filter = DataSetQueryHelper.ActiveFilterFor(nameof(FieldMappingTransformConfiguration.Id), transformId)
                },
                new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformContainerName),
                ct)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.ToNewResult<FieldMappingTransformPayload?>();
        }

        var found = (result.Value ?? []).FirstOrDefault();
        if (found is null)
        {
            return GenericResult<FieldMappingTransformPayload?>.Success(null);
        }

        return GenericResult<FieldMappingTransformPayload?>.Success(new FieldMappingTransformPayload
        {
            Id = found.Id,
            FieldMappingId = found.DataSetFieldMappingId,
            TransformType = found.TransformType,
            Ordinal = found.Ordinal
        });
    }
}
