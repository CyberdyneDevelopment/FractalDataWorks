using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration.Logging;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Finds the glossary terms closest to a text, asking the term provider for ordered windows and for a count, so the whole glossary is never
/// read to rank it (FDW-2312).
/// </summary>
/// <remarks>
/// <para>
/// The windows are those of <c>RankedRowSearch</c>: the names that start with the text, then (only when that window is short) the names that
/// contain it and the definitions or acronym expansions that contain it, twice the window each. Each term is ranked again here by
/// <see cref="SearchRank"/> over its name and its description (what an acronym stands for, then the definition), so a term ranks the same
/// as every other kind.
/// </para>
/// <para>
/// A term is a domain row, read as the caller by the provider, so a term the caller may not see is not found and not counted. Terms with
/// the same name come in the order the provider gives them.
/// </para>
/// </remarks>
internal sealed class GlossaryTermSearch : IRankedSearch
{
    private readonly IGlossaryTermConfigurationProvider _terms;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="GlossaryTermSearch"/> class.</summary>
    /// <param name="terms">Supplies the glossary terms.</param>
    /// <param name="logger">The logger.</param>
    internal GlossaryTermSearch(IGlossaryTermConfigurationProvider terms, ILogger? logger)
    {
        _terms = terms;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Kind => CatalogKinds.GlossaryTerm;

    /// <inheritdoc />
    public int MaxWindow => CatalogSearchLimits.MaxWindow;

    /// <inheritdoc />
    public async Task<IGenericResult<SearchPage>> Search(string text, int window, CancellationToken cancellationToken = default)
    {
        if (window < 1 || window > MaxWindow)
        {
            return GenericResult<SearchPage>.Failure(ConfigurationRowReaderLog.WindowRefused(_logger, nameof(GlossaryTermDomainConfiguration), window, MaxWindow));
        }

        var found = new Dictionary<Guid, Found>();
        var matches = text.Length == 0
            ? await Browse(window, found, cancellationToken).ConfigureAwait(false)
            : await Find(text, window, found, cancellationToken).ConfigureAwait(false);
        if (matches.IsFailure)
        {
            return matches.ToNewResult<SearchPage>();
        }

        var closest = found.Values
            .OrderBy(candidate => candidate.Hit.Rank)
            .ThenBy(candidate => candidate.Hit.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Term.Id)
            .Take(window)
            .Select(candidate => candidate.Hit)
            .ToList();
        return GenericResult<SearchPage>.Success(new SearchPage(closest, matches.Value));
    }

    /// <inheritdoc />
    /// <remarks>A term sits under nothing, so it is placed as it is.</remarks>
    public Task<IGenericResult<IReadOnlyList<SearchHit>>> Place(IReadOnlyList<SearchHit> hits, CancellationToken cancellationToken = default) =>
        Task.FromResult<IGenericResult<IReadOnlyList<SearchHit>>>(GenericResult<IReadOnlyList<SearchHit>>.Success(hits));

    // No text: every term matches equally, so the first terms by name are the answer.
    private async Task<IGenericResult<int>> Browse(
        int window, Dictionary<Guid, Found> found, CancellationToken cancellationToken)
    {
        var read = await Read(null, window, true, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<int>();
        }

        Add(found, read.Value!.Terms, string.Empty);
        return Counted(read.Value.Total);
    }

    private async Task<IGenericResult<int>> Find(
        string text, int window, Dictionary<Guid, Found> found, CancellationToken cancellationToken)
    {
        var starting = await Read(Condition(nameof(GlossaryTermDomainConfiguration.Name), "StartsWith", text), window, false, cancellationToken).ConfigureAwait(false);
        if (starting.IsFailure)
        {
            return starting.ToNewResult<int>();
        }

        Add(found, starting.Value!.Terms, text);

        // A full window of names that start with the text holds the closest terms; only a short one leaves room for the wider classes.
        if (starting.Value!.Terms.Count < window)
        {
            var inName = await Read(Condition(nameof(GlossaryTermDomainConfiguration.Name), "Contains", text), 2 * window, false, cancellationToken).ConfigureAwait(false);
            if (inName.IsFailure)
            {
                return inName.ToNewResult<int>();
            }

            Add(found, inName.Value!.Terms, text);

            var inDescription = await Read(InDescription(text), 2 * window, false, cancellationToken).ConfigureAwait(false);
            if (inDescription.IsFailure)
            {
                return inDescription.ToNewResult<int>();
            }

            Add(found, inDescription.Value!.Terms, text);
        }

        var counted = await Read(Either(text), 1, true, cancellationToken).ConfigureAwait(false);
        return counted.IsFailure ? counted.ToNewResult<int>() : Counted(counted.Value!.Total);
    }

    private IGenericResult<int> Counted(int? total) =>
        total is { } counted
            ? GenericResult<int>.Success(counted)
            : GenericResult<int>.Failure(CatalogSearchLog.TermsNotCounted(_logger));

    private async Task<IGenericResult<TermsRead>> Read(
        IFilterNode? condition, int take, bool includeTotal, CancellationToken cancellationToken)
    {
        var filter = condition is null ? null : new FilterExpression { Root = condition };
        var page = await _terms.Get(new DomainQuery(filter, ByName(), 0, take, includeTotal), cancellationToken).ConfigureAwait(false);
        if (page.IsFailure)
        {
            return page.ToNewResult<TermsRead>();
        }

        var terms = new List<GlossaryTermDomainConfiguration>();
        await foreach (var term in page.Value!.Items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            terms.Add(term);
        }

        return page.Value.Outcome.IsFailure
            ? page.Value.Outcome.ToNewResult<TermsRead>()
            : GenericResult<TermsRead>.Success(new TermsRead(terms, page.Value.Total));
    }

    // Ranks each term here, by the rule every source shares, and keeps one of a term that more than one window read. A term the store
    // finds that the rule does not (the store compares accents or width differently) ranks 3: it matched, and nothing about it is closer.
    private static void Add(Dictionary<Guid, Found> found, List<GlossaryTermDomainConfiguration> terms, string text)
    {
        foreach (var term in terms)
        {
            var description = term.StandsFor is { Length: > 0 } standsFor ? string.Concat(standsFor, ". ", term.Definition) : term.Definition;
            var rank = SearchRank.Of(term.Name, description, text);
            found[term.Id] = new Found(term, new SearchHit(
                term.Name, description, 0, rank == SearchRank.NoMatch ? 3 : rank, string.Empty, 0, string.IsNullOrWhiteSpace(term.Owner) ? null : term.Owner));
        }
    }

    // The terms one read returned, and the number the store counted when it was asked to.
    private sealed record TermsRead(List<GlossaryTermDomainConfiguration> Terms, int? Total);

    // A term and the hit it makes, kept together so the hits can be ordered by the term's key.
    private sealed record Found(GlossaryTermDomainConfiguration Term, SearchHit Hit);

    private static FilterCondition Condition(string property, string operatorName, string text) =>
        new() { PropertyName = property, Operator = FilterOperators.ByName(operatorName), Value = text };

    // The definition contains the text, or what an acronym stands for does.
    private static FilterGroup InDescription(string text) => new()
    {
        Operator = LogicalOperator.Or,
        Nodes =
        [
            Condition(nameof(GlossaryTermDomainConfiguration.Definition), "Contains", text),
            Condition(nameof(GlossaryTermDomainConfiguration.StandsFor), "Contains", text),
        ],
    };

    // The name, definition or acronym expansion contains the text: the terms the text finds, whatever their rank.
    private static FilterGroup Either(string text) => new()
    {
        Operator = LogicalOperator.Or,
        Nodes =
        [
            Condition(nameof(GlossaryTermDomainConfiguration.Name), "Contains", text),
            Condition(nameof(GlossaryTermDomainConfiguration.Definition), "Contains", text),
            Condition(nameof(GlossaryTermDomainConfiguration.StandsFor), "Contains", text),
        ],
    };

    private static OrderingExpression ByName() => new()
    {
        OrderedFields = [new OrderedField { PropertyName = nameof(GlossaryTermDomainConfiguration.Name), Direction = SortDirections.ByName("Ascending") }],
    };
}
