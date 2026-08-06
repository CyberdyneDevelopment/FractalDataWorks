using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Quality.Services;

/// <summary>
/// Implementation of data catalog and business glossary operations.
/// </summary>
public sealed class CatalogService : ICatalogService
{
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<List<GlossaryTermConfiguration>> _termsMonitor;
    private readonly IOptionsMonitor<List<DataSetAnnotationConfiguration>> _annotationsMonitor;
    private readonly List<GlossaryTermConfiguration> _inMemoryTerms = new();
    private readonly Dictionary<string, DataSetAnnotationConfiguration> _inMemoryAnnotations = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogService"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="termsMonitor">The glossary terms configuration monitor.</param>
    /// <param name="annotationsMonitor">The annotations configuration monitor.</param>
    public CatalogService(
        ILoggerFactory loggerFactory,
        IOptionsMonitor<List<GlossaryTermConfiguration>> termsMonitor,
        IOptionsMonitor<List<DataSetAnnotationConfiguration>> annotationsMonitor)
    {
        _logger = loggerFactory.CreateLogger<CatalogService>();
        _termsMonitor = termsMonitor;
        _annotationsMonitor = annotationsMonitor;
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<GlossaryTermConfiguration>>> SearchTerms(string? query, string? category, CancellationToken ct = default)
    {
        CatalogLog.SearchingTerms(_logger, query ?? string.Empty, category);

        var results = _inMemoryTerms.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            results = results.Where(t =>
                t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Definition.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            results = results.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        var list = results.ToList();
        return Task.FromResult(GenericResult<IReadOnlyList<GlossaryTermConfiguration>>.Success(list));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<GlossaryTermConfiguration>> CreateTerm(GlossaryTermConfiguration term, CancellationToken ct = default)
    {
        try
        {
            if (_inMemoryTerms.Any(t => t.Name.Equals(term.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(GenericResult<GlossaryTermConfiguration>.Failure(
                    CatalogLog.DuplicateTermName(_logger, term.Name)));
            }

            term.Id = term.Id == Guid.Empty ? Guid.NewGuid() : term.Id;
            _inMemoryTerms.Add(term);

            CatalogLog.TermCreated(_logger, term.Name, term.Category);
            return Task.FromResult(GenericResult<GlossaryTermConfiguration>.Success(term));
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<GlossaryTermConfiguration>.Failure(
                CatalogLog.OperationFailed(_logger, ex, "CreateTerm")));
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<GlossaryTermConfiguration>> UpdateTerm(Guid id, GlossaryTermConfiguration term, CancellationToken ct = default)
    {
        var existing = _inMemoryTerms.FirstOrDefault(t => t.Id == id);
        if (existing == null)
        {
            return Task.FromResult(GenericResult<GlossaryTermConfiguration>.Failure(
                CatalogLog.TermNotFound(_logger, id)));
        }

        _inMemoryTerms.Remove(existing);
        term.Id = id;
        _inMemoryTerms.Add(term);

        CatalogLog.TermUpdated(_logger, term.Name);
        return Task.FromResult(GenericResult<GlossaryTermConfiguration>.Success(term));
    }

    /// <inheritdoc/>
    public Task<IGenericResult> DeleteTerm(Guid id, CancellationToken ct = default)
    {
        var existing = _inMemoryTerms.FirstOrDefault(t => t.Id == id);
        if (existing == null)
        {
            return Task.FromResult(GenericResult.Failure(
                CatalogLog.TermNotFound(_logger, id)));
        }

        _inMemoryTerms.Remove(existing);
        CatalogLog.TermDeleted(_logger, existing.Name);
        return Task.FromResult(GenericResult.Success());
    }

    /// <inheritdoc/>
    public Task<IGenericResult<GlossaryTermConfiguration>> GetTerm(Guid id, CancellationToken ct = default)
    {
        var term = _inMemoryTerms.FirstOrDefault(t => t.Id == id);
        if (term == null)
        {
            return Task.FromResult(GenericResult<GlossaryTermConfiguration>.Failure(
                CatalogLog.TermNotFound(_logger, id)));
        }

        return Task.FromResult(GenericResult<GlossaryTermConfiguration>.Success(term));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<DataSetAnnotationConfiguration>> GetAnnotation(string dataSetName, CancellationToken ct = default)
    {
        CatalogLog.LoadingAnnotation(_logger, dataSetName);

        if (!_inMemoryAnnotations.TryGetValue(dataSetName, out var annotation))
        {
            return Task.FromResult(GenericResult<DataSetAnnotationConfiguration>.Failure(
                CatalogLog.AnnotationNotFound(_logger, dataSetName)));
        }

        return Task.FromResult(GenericResult<DataSetAnnotationConfiguration>.Success(annotation));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<DataSetAnnotationConfiguration>> UpdateAnnotation(string dataSetName, DataSetAnnotationConfiguration annotation, CancellationToken ct = default)
    {
        annotation.DataSetName = dataSetName;
        _inMemoryAnnotations[dataSetName] = annotation;

        var owner = annotation.BusinessOwner ?? annotation.TechnicalOwner ?? "Unknown";
        CatalogLog.AnnotationUpdated(_logger, dataSetName);
        return Task.FromResult(GenericResult<DataSetAnnotationConfiguration>.Success(annotation));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<CatalogSearchResult>>> Search(string query, CancellationToken ct = default)
    {
        var results = new List<CatalogSearchResult>();

        // Search glossary terms
        var matchingTerms = _inMemoryTerms.Where(t =>
            t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            t.Definition.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var term in matchingTerms)
        {
            var relevance = CalculateRelevance(query, term.Name, term.Definition);
            results.Add(new CatalogSearchResult("GlossaryTerm", term.Name, term.Definition, relevance));
        }

        // Search annotations
        var matchingAnnotations = _inMemoryAnnotations.Values.Where(a =>
            a.DataSetName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            (a.Description != null && a.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));

        foreach (var annotation in matchingAnnotations)
        {
            var relevance = CalculateRelevance(query, annotation.DataSetName, annotation.Description);
            results.Add(new CatalogSearchResult("DataSet", annotation.DataSetName, annotation.Description, relevance));
        }

        var sortedResults = results.OrderByDescending(r => r.Relevance).ToList();
        CatalogLog.SearchCompleted(_logger, query, sortedResults.Count);

        return Task.FromResult(GenericResult<IReadOnlyList<CatalogSearchResult>>.Success(sortedResults));
    }

    private static double CalculateRelevance(string query, string name, string? description)
    {
        double score = 0.0;

        if (name.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 1.0;
        }
        else if (name.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.7;
        }

        if (description != null && description.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.3;
        }

        return score;
    }
}
