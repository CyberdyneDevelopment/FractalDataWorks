namespace Fdw.Services.Catalog.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for data catalog and search endpoints.
/// </summary>
public sealed class CatalogApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogApiClient"/> class.
    /// </summary>
    public CatalogApiClient(HttpClient httpClient, ILogger<CatalogApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Searches the data catalog.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="entityType">Optional entity type filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of matching catalog entities.</returns>
    public Task<IGenericResult<IReadOnlyList<CatalogEntityPayload>>> Search(string query, string? entityType = null, CancellationToken ct = default)
    {
        var path = $"catalog/search?query={Uri.EscapeDataString(query)}";
        if (!string.IsNullOrEmpty(entityType))
        {
            path += $"&entityType={Uri.EscapeDataString(entityType)}";
        }
        return GetList<CatalogEntityPayload>(path, ct);
    }

    /// <summary>
    /// Gets all glossary terms.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of glossary terms.</returns>
    public Task<IGenericResult<IReadOnlyList<GlossaryTermPayload>>> GetGlossary(CancellationToken ct = default)
        => GetList<GlossaryTermPayload>("catalog/glossary", ct);

    /// <summary>
    /// Gets the catalog entry for a specific DataSet.
    /// </summary>
    /// <param name="name">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the DataSet catalog details.</returns>
    public Task<IGenericResult<DataSetCatalogPayload>> GetDataSetEntry(string name, CancellationToken ct = default)
        => Get<DataSetCatalogPayload>($"catalog/datasets/{name}", ct);

    /// <summary>
    /// Gets all annotations for a specific DataSet.
    /// </summary>
    /// <param name="dataSetName">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of annotations.</returns>
    public Task<IGenericResult<IReadOnlyList<DataSetAnnotationPayload>>> GetAnnotations(string dataSetName, CancellationToken ct = default)
        => GetList<DataSetAnnotationPayload>($"catalog/datasets/{Uri.EscapeDataString(dataSetName)}/annotations", ct);

    /// <summary>
    /// Creates a new annotation for a DataSet.
    /// </summary>
    /// <param name="dataSetName">The DataSet name.</param>
    /// <param name="request">The annotation data to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created annotation.</returns>
    public Task<IGenericResult<DataSetAnnotationPayload>> CreateAnnotation(string dataSetName, CreateAnnotationRequest request, CancellationToken ct = default)
        => Post<CreateAnnotationRequest, DataSetAnnotationPayload>($"catalog/datasets/{Uri.EscapeDataString(dataSetName)}/annotations", request, ct);

    /// <summary>
    /// Deletes a DataSet annotation.
    /// </summary>
    /// <param name="annotationId">The unique identifier of the annotation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure of the deletion.</returns>
    public Task<IGenericResult> DeleteAnnotation(Guid annotationId, CancellationToken ct = default)
        => Delete($"catalog/annotations/{annotationId}", ct);

    /// <summary>
    /// Resolves (marks as reviewed) a DataSet annotation.
    /// </summary>
    /// <param name="annotationId">The unique identifier of the annotation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the resolved annotation.</returns>
    public Task<IGenericResult<DataSetAnnotationPayload>> ResolveAnnotation(Guid annotationId, CancellationToken ct = default)
        => PostWithResponse<DataSetAnnotationPayload>($"catalog/annotations/{annotationId}/resolve", ct);

    /// <summary>
    /// Searches glossary terms.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of matching glossary terms.</returns>
    public Task<IGenericResult<IReadOnlyList<GlossaryTermPayload>>> SearchTerms(string query, CancellationToken ct = default)
        => GetList<GlossaryTermPayload>($"catalog/glossary?query={Uri.EscapeDataString(query)}", ct);

    /// <summary>
    /// Gets a specific glossary term by identifier.
    /// </summary>
    /// <param name="id">The glossary term identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the glossary term.</returns>
    public Task<IGenericResult<GlossaryTermPayload>> GetTerm(Guid id, CancellationToken ct = default)
        => Get<GlossaryTermPayload>($"catalog/glossary/{id}", ct);

    /// <summary>
    /// Creates a new glossary term.
    /// </summary>
    /// <param name="request">The glossary term data to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created glossary term.</returns>
    public Task<IGenericResult<GlossaryTermPayload>> CreateTerm(CreateGlossaryTermRequest request, CancellationToken ct = default)
        => Post<CreateGlossaryTermRequest, GlossaryTermPayload>("catalog/glossary", request, ct);

    /// <summary>
    /// Updates an existing glossary term.
    /// </summary>
    /// <param name="id">The glossary term identifier.</param>
    /// <param name="request">The updated glossary term data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated glossary term.</returns>
    public Task<IGenericResult<GlossaryTermPayload>> UpdateTerm(Guid id, UpdateGlossaryTermRequest request, CancellationToken ct = default)
        => Patch<UpdateGlossaryTermRequest, GlossaryTermPayload>($"catalog/glossary/{id}", request, ct);

    /// <summary>
    /// Deletes a glossary term.
    /// </summary>
    /// <param name="id">The glossary term identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the deletion succeeded.</returns>
    public Task<IGenericResult> DeleteTerm(Guid id, CancellationToken ct = default)
        => Delete($"catalog/glossary/{id}", ct);
}
