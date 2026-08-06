using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Services;

/// <summary>
/// Service interface for data catalog and business glossary operations.
/// </summary>
public interface ICatalogService
{
    /// <summary>
    /// Searches glossary terms.
    /// </summary>
    /// <param name="query">Optional search query.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing matching glossary terms.</returns>
    Task<IGenericResult<IReadOnlyList<GlossaryTermConfiguration>>> SearchTerms(string? query, string? category, CancellationToken ct = default);

    /// <summary>
    /// Creates a new glossary term.
    /// </summary>
    /// <param name="term">The term configuration to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the created term configuration.</returns>
    Task<IGenericResult<GlossaryTermConfiguration>> CreateTerm(GlossaryTermConfiguration term, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing glossary term.
    /// </summary>
    /// <param name="id">The term identifier.</param>
    /// <param name="term">The updated term configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the updated term configuration.</returns>
    Task<IGenericResult<GlossaryTermConfiguration>> UpdateTerm(Guid id, GlossaryTermConfiguration term, CancellationToken ct = default);

    /// <summary>
    /// Deletes a glossary term.
    /// </summary>
    /// <param name="id">The term identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<IGenericResult> DeleteTerm(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a glossary term by identifier.
    /// </summary>
    /// <param name="id">The term identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the term configuration.</returns>
    Task<IGenericResult<GlossaryTermConfiguration>> GetTerm(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the annotation for a DataSet.
    /// </summary>
    /// <param name="dataSetName">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the annotation configuration.</returns>
    Task<IGenericResult<DataSetAnnotationConfiguration>> GetAnnotation(string dataSetName, CancellationToken ct = default);

    /// <summary>
    /// Updates the annotation for a DataSet.
    /// </summary>
    /// <param name="dataSetName">The DataSet name.</param>
    /// <param name="annotation">The updated annotation configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the updated annotation configuration.</returns>
    Task<IGenericResult<DataSetAnnotationConfiguration>> UpdateAnnotation(string dataSetName, DataSetAnnotationConfiguration annotation, CancellationToken ct = default);

    /// <summary>
    /// Searches the catalog for items matching a query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing matching catalog items.</returns>
    Task<IGenericResult<IReadOnlyList<CatalogSearchResult>>> Search(string query, CancellationToken ct = default);
}