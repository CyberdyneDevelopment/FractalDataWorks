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
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for soft-deleting a field mapping transform.
/// Provides virtual default implementations using IDataGateway for reads and writes.
/// </summary>
/// <remarks>
/// Why: FieldMappingTransform is a child record that does not implement IGenericConfiguration,
/// so it cannot be routed through IConfigurationWriter. ConfigurationDeleteCommand via IDataGateway
/// is the documented exception for child records. The configuration connection name and path are
/// sourced from DataSetConfigurationProvider rather than IConfigurationConnectionNameProvider.
/// </remarks>
public abstract class DeleteFieldMappingTransformEndpointBase : CrudDeleteEndpointBase<DeleteFieldMappingTransformRequest>
{
    /// <summary>Gets the data gateway for executing queries and commands.</summary>
    protected IDataGateway DataGateway { get; }

    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <inheritdoc />
    protected DeleteFieldMappingTransformEndpointBase(
        IDataGateway dataGateway,
        DataSetConfigurationProvider dataSetProvider)
    {
        DataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "field-mapping-transforms";

    /// <summary>Gets the delete policy for this endpoint.</summary>
    protected override string DeletePolicy => "datasets:delete";

    /// <summary>Gets the route for this endpoint.</summary>
    protected override string Route => "/field-mappings/{FieldMappingId}/transforms/{TransformId}";

    /// <summary>Gets the OpenAPI summary for this endpoint.</summary>
    protected override string EndpointSummary => "Delete a field mapping transform";

    /// <summary>Gets the OpenAPI description for this endpoint.</summary>
    protected override string EndpointDescription =>
        "Soft-deletes a transform step from a field mapping's transform chain.";

    /// <summary>Gets the connection name for configuration database queries.</summary>
    protected virtual string ConfigurationConnectionName => _dataSetProvider.DataStoreName;

    /// <summary>
    /// Gets the configuration path that holds the transform containers.
    /// </summary>
    protected virtual string TransformPathName => "transform";

    /// <summary>Gets the container name for FieldMappingTransform queries.</summary>
    protected virtual string TransformContainerName => "FieldMappingTransform";

    /// <summary>Returns the transform identifier from the delete request.</summary>
    protected override string GetResourceIdentifier(DeleteFieldMappingTransformRequest request)
        => request.TransformId.ToString();

    /// <summary>Checks if the transform exists.</summary>
    protected override Task<IGenericResult<bool>> CheckExistsForDelete(DeleteFieldMappingTransformRequest request, CancellationToken ct)
    {
        return CheckTransformExists(request.TransformId, ct);
    }

    /// <summary>Soft-deletes the transform.</summary>
    protected override Task<IGenericResult> Delete(DeleteFieldMappingTransformRequest request, CancellationToken ct)
    {
        return DeleteTransform(request.TransformId, ct);
    }

    /// <summary>
    /// Checks if a transform exists.
    /// Default implementation queries via IDataGateway. Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<bool>> CheckTransformExists(Guid transformId, CancellationToken ct)
    {
        var command = new QueryCommand<FieldMappingTransformConfiguration>
        {
            Filter = DataSetQueryHelper.ByParentIdFilter(nameof(FieldMappingTransformConfiguration.Id), transformId)
        };
        var result = await DataGateway
            .Execute<IEnumerable<FieldMappingTransformConfiguration>>(
                command, new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformContainerName), ct)
            .ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<bool>();

        return GenericResult<bool>.Success(result.Value?.Any() == true);
    }

    /// <summary>
    /// Soft-deletes the transform via a ConfigurationDeleteCommand through the DataGateway.
    /// Default implementation uses IDataGateway. Override for custom behavior.
    /// </summary>
    protected virtual async Task<IGenericResult> DeleteTransform(Guid transformId, CancellationToken ct)
    {
        FieldMappingTransformEndpointLog.DeletingTransform(Logger, transformId);

        var deleteResult = await DataGateway
            .Execute<int>(
                new ConfigurationDeleteCommand(transformId),
                new DataStoreTarget(ConfigurationConnectionName, TransformPathName, TransformContainerName),
                ct)
            .ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            FieldMappingTransformEndpointLog.DeleteTransformFailed(Logger, transformId);
            return deleteResult;
        }

        FieldMappingTransformEndpointLog.DeletedTransform(Logger, transformId);
        return GenericResult.Success();
    }
}
