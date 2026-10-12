namespace Fdw.Services.Data.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.Clients.Abstractions;

/// <summary>
/// API client for DataSet management endpoints.
/// </summary>
public class DataSetApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public DataSetApiClient(HttpClient httpClient, ILogger<DataSetApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets all configured DataSets.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of DataSet summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<DataSetSummaryPayload>>> GetDataSets(CancellationToken ct = default)
        => GetList<DataSetSummaryPayload>("datasets", ct);

    /// <summary>
    /// Gets a specific DataSet by name.
    /// </summary>
    /// <param name="name">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the DataSet details.</returns>
    public virtual Task<IGenericResult<DataSetDetailPayload>> GetDataSet(string name, CancellationToken ct = default)
        => Get<DataSetDetailPayload>($"datasets/{name}", ct);

    /// <summary>
    /// Creates a new DataSet.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created DataSet details.</returns>
    public virtual Task<IGenericResult<DataSetDetailPayload>> CreateDataSet(CreateDataSetPayload request, CancellationToken ct = default)
        => Post<CreateDataSetPayload, DataSetDetailPayload>("datasets", request, ct);

    /// <summary>
    /// Updates an existing DataSet.
    /// </summary>
    /// <param name="name">The DataSet name.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated DataSet details.</returns>
    public virtual Task<IGenericResult<DataSetDetailPayload>> UpdateDataSet(string name, UpdateDataSetPayload request, CancellationToken ct = default)
        => Patch<UpdateDataSetPayload, DataSetDetailPayload>($"datasets/{name}", request, ct);

    /// <summary>
    /// Deletes a DataSet.
    /// </summary>
    /// <param name="name">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the deletion succeeded.</returns>
    public virtual Task<IGenericResult> DeleteDataSet(string name, CancellationToken ct = default)
        => Delete($"datasets/{name}", ct);

    /// <summary>
    /// Previews data from a DataSet with optional pagination and filter conditions.
    /// </summary>
    /// <param name="name">The DataSet name.</param>
    /// <param name="request">The preview request specifying page, page size, and filter conditions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the preview data with columns and rows.</returns>
    public virtual Task<IGenericResult<DataPreviewResponsePayload>> PreviewDataSet(string name, DataPreviewRequestPayload request, CancellationToken ct = default)
        => Get<DataPreviewResponsePayload>($"datasets/{name}/preview?maxRows={request.MaxRows}", ct);

    /// <summary>
    /// Gets all transforms for a field mapping.
    /// </summary>
    /// <param name="fieldMappingId">The field mapping identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of transforms.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<FieldMappingTransformPayload>>> GetTransforms(Guid fieldMappingId, CancellationToken ct = default)
        => GetList<FieldMappingTransformPayload>($"field-mappings/{fieldMappingId}/transforms", ct);

    /// <summary>
    /// Attaches an existing transformation to a field mapping.
    /// </summary>
    /// <param name="request">The field mapping, transformation name and chain position.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the attachment.</returns>
    public virtual Task<IGenericResult<FieldMappingTransformPayload>> SaveTransform(SaveFieldMappingTransformRequest request, CancellationToken ct = default)
        => Post<SaveFieldMappingTransformRequest, FieldMappingTransformPayload>($"field-mappings/{request.FieldMappingId}/transforms", request, ct);

    /// <summary>
    /// Detaches a transformation from a field mapping. The transformation itself is not deleted.
    /// </summary>
    /// <param name="fieldMappingId">The field mapping.</param>
    /// <param name="transformationName">The attached transformation's name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether it was detached.</returns>
    public virtual Task<IGenericResult> DeleteTransform(Guid fieldMappingId, string transformationName, CancellationToken ct = default)
        => Delete($"field-mappings/{fieldMappingId}/transforms/{Uri.EscapeDataString(transformationName)}", ct);

    /// <summary>
    /// Reorders transforms for a field mapping.
    /// </summary>
    /// <param name="fieldMappingId">The field mapping identifier.</param>
    /// <param name="request">Every attached transformation's name, in the new order.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the reorder succeeded.</returns>
    public virtual Task<IGenericResult> ReorderTransforms(Guid fieldMappingId, ReorderTransformsRequest request, CancellationToken ct = default)
        => Post<ReorderTransformsRequest>($"field-mappings/{fieldMappingId}/transforms/reorder", request, ct);

    /// <summary>Lists the transformations (defined once, attachable to field mappings).</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing every transformation.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<TransformationPayload>>> ListTransformations(CancellationToken ct = default)
        => GetList<TransformationPayload>("transforms", ct);

    /// <summary>Gets one transformation by name.</summary>
    /// <param name="name">The transformation's name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the transformation.</returns>
    public virtual Task<IGenericResult<TransformationPayload>> GetTransformation(string name, CancellationToken ct = default)
        => Get<TransformationPayload>($"transforms/{Uri.EscapeDataString(name)}", ct);

    /// <summary>Defines a transformation.</summary>
    /// <param name="request">Its name, operation and parameter values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the transformation as saved.</returns>
    public virtual Task<IGenericResult<TransformationPayload>> CreateTransformation(CreateTransformationRequest request, CancellationToken ct = default)
        => Post<CreateTransformationRequest, TransformationPayload>("transforms", request, ct);

    /// <summary>Changes a transformation; omitted members keep their value.</summary>
    /// <param name="request">The change, addressed by <see cref="UpdateTransformationRequest.Name"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the transformation as saved.</returns>
    public virtual Task<IGenericResult<TransformationPayload>> UpdateTransformation(UpdateTransformationRequest request, CancellationToken ct = default)
        => Patch<UpdateTransformationRequest, TransformationPayload>($"transforms/{Uri.EscapeDataString(request.Name)}", request, ct);

    /// <summary>Deletes a transformation. Refused while it is attached to a field mapping.</summary>
    /// <param name="name">The transformation's name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether it was deleted.</returns>
    public virtual Task<IGenericResult> DeleteTransformation(string name, CancellationToken ct = default)
        => Delete($"transforms/{Uri.EscapeDataString(name)}", ct);

    /// <summary>
    /// Gets all available transform types.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of available transform types.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<TransformTypePayload>>> GetAvailableTransformTypes(CancellationToken ct = default)
        => GetList<TransformTypePayload>("transform-types", ct);

    /// <summary>
    /// Gets all available DataSet types from the server's DataSetTypes TypeCollection.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of DataSet type summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<DataSetTypeSummaryPayload>>> GetTypes(CancellationToken ct = default)
        => GetList<DataSetTypeSummaryPayload>("datasets/types", ct);
}
