using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Finds the named things of the catalog by name or description (FDW-1681): one search over data sets and their fields, data stores
/// with their paths, containers and fields, calculations, transformations, pipelines and glossary terms.
/// </summary>
/// <remarks>
/// <para>
/// A kind with few members (data sets, stores, paths, containers, calculations, transformations, pipelines) is read whole and ranked here.
/// A kind that can have very many (the fields of data sets and of containers, the glossary terms) is an <see cref="IRankedSearch"/>: the
/// store returns its closest <c>skip + take</c> matches in order and counts the rest, so the cost of a page does not grow with the
/// catalog. The page is chosen from the two by <see cref="SearchRank"/>, then kind, then name, and only the fields of that page are
/// asked where they sit.
/// </para>
/// <para>
/// Each kind is read as the caller, so a thing the caller may not see is not found and not counted. A read that fails fails the search: a
/// kind is never left out of an answer that claims to have searched it. A field whose parent the caller cannot read is left out of the
/// page after the page was chosen, so a page can hold a little fewer than <c>take</c> while <c>hasMore</c> is true.
/// </para>
/// </remarks>
internal sealed class CatalogSearch
{
    // The kinds that are searched at the store, a page's worth at a time.
    private static readonly string[] RankedKinds = [CatalogKinds.DataSetField, CatalogKinds.ContainerField, CatalogKinds.GlossaryTerm];

    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataStoreProvider _dataStores;
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly ICalculationConfigurationProvider _calculations;
    private readonly ITransformationConfigurationProvider _transformations;
    private readonly Dictionary<string, IRankedSearch> _searches;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="CatalogSearch"/> class.</summary>
    /// <param name="dataSets">Supplies the data sets.</param>
    /// <param name="dataStores">Supplies the data stores with their paths and containers.</param>
    /// <param name="pipelines">Supplies the pipelines.</param>
    /// <param name="calculations">Supplies the calculations.</param>
    /// <param name="transformations">Supplies the transformations.</param>
    /// <param name="searches">The searches of the kinds found at the store, one for each kind.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentException">Two searches serve one kind, or a search serves something that is not a catalog kind.</exception>
    internal CatalogSearch(
        IDataSetConfigurationProvider dataSets,
        IDataStoreProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        ICalculationConfigurationProvider calculations,
        ITransformationConfigurationProvider transformations,
        IEnumerable<IRankedSearch> searches,
        ILogger? logger)
    {
        _dataSets = dataSets;
        _dataStores = dataStores;
        _pipelines = pipelines;
        _calculations = calculations;
        _transformations = transformations;
        _searches = searches.ToDictionary(search => search.Kind, StringComparer.Ordinal);
        _logger = logger ?? NullLogger.Instance;
        foreach (var kind in _searches.Keys.Where(kind => !RankedKinds.Contains(kind, StringComparer.Ordinal)))
        {
            throw new ArgumentException($"'{kind}' is not a kind the catalog searches at the store; they are {string.Join(", ", RankedKinds)}", nameof(searches));
        }
    }

    /// <summary>Runs a search whose request has already been checked.</summary>
    /// <param name="kinds">The kinds to search.</param>
    /// <param name="text">What to find; empty lists everything.</param>
    /// <param name="skip">How many matches to skip.</param>
    /// <param name="take">How many matches to return.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    /// <returns>The page, or the failure of the first read that could not be answered.</returns>
    internal async Task<IGenericResult<CatalogSearchResponse>> Run(
        CatalogKindSet kinds, string text, int skip, int take, CancellationToken cancellationToken)
    {
        var sources = Sources(kinds, skip + take);
        if (sources.IsFailure)
        {
            return sources.ToNewResult<CatalogSearchResponse>();
        }

        var candidates = new List<CatalogCandidate>();
        var total = 0;
        foreach (var read in Reads(kinds))
        {
            var found = await read(cancellationToken).ConfigureAwait(false);
            if (found.IsFailure)
            {
                return found.ToNewResult<CatalogSearchResponse>();
            }

            foreach (var entry in found.Value!)
            {
                var rank = SearchRank.Of(entry.Name, entry.Description, text);
                if (rank != SearchRank.NoMatch)
                {
                    candidates.Add(new CatalogCandidate(rank, entry.EntityType, entry.Name, entry.Location ?? string.Empty, 0, entry, null, null));
                    total++;
                }
            }
        }

        foreach (var source in sources.Value!)
        {
            var found = await source.Search(text, skip + take, cancellationToken).ConfigureAwait(false);
            if (found.IsFailure)
            {
                return found.ToNewResult<CatalogSearchResponse>();
            }

            total += found.Value!.Matches;
            candidates.AddRange(found.Value.Hits.Select((hit, order) =>
                new CatalogCandidate(hit.Rank, source.Kind, hit.Name, string.Empty, order, null, source, hit)));
        }

        var page = candidates
            .OrderBy(candidate => candidate.Rank)
            .ThenBy(candidate => CatalogKinds.IndexOf(candidate.Kind))
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Tie, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Order)
            .Skip(skip)
            .Take(take)
            .ToList();
        var items = await Place(page, cancellationToken).ConfigureAwait(false);
        if (items.IsFailure)
        {
            return items.ToNewResult<CatalogSearchResponse>();
        }

        CatalogSearchLog.Searched(_logger, text, kinds.Names.Count, total);
        return GenericResult<CatalogSearchResponse>.Success(new CatalogSearchResponse
        {
            Items = items.Value!,
            Skip = skip,
            Take = take,
            Total = total,
            HasMore = total > skip + take,
            Kinds = kinds.Names.ToList(),
        });
    }

    // The searches of the stored kinds that were asked for. A kind with no search, or one that reads fewer matches than the page needs, is a
    // mistake of the host and fails the search rather than leaving the kind out.
    private IGenericResult<List<IRankedSearch>> Sources(CatalogKindSet kinds, int window)
    {
        var sources = new List<IRankedSearch>();
        foreach (var kind in RankedKinds.Where(kinds.Wants))
        {
            if (!_searches.TryGetValue(kind, out var source))
            {
                return GenericResult<List<IRankedSearch>>.Failure(CatalogSearchLog.NoSourceForKind(_logger, kind));
            }

            if (window > source.MaxWindow)
            {
                return GenericResult<List<IRankedSearch>>.Failure(CatalogSearchLog.WindowPastSource(_logger, kind, window, source.MaxWindow));
            }

            sources.Add(source);
        }

        return GenericResult<List<IRankedSearch>>.Success(sources);
    }

    // Names where the hits of a page sit, one question to each source for the hits it found, then makes the entries in the page's order.
    private static async Task<IGenericResult<List<CatalogEntryDto>>> Place(List<CatalogCandidate> page, CancellationToken cancellationToken)
    {
        var placed = new Dictionary<SearchHit, SearchHit>(ReferenceEqualityComparer.Instance);
        foreach (var group in page.Where(candidate => candidate.Hit is not null).GroupBy(candidate => candidate.Source!))
        {
            var hits = group.Select(candidate => candidate.Hit!).ToList();
            var located = await group.Key.Place(hits, cancellationToken).ConfigureAwait(false);
            if (located.IsFailure)
            {
                return located.ToNewResult<List<CatalogEntryDto>>();
            }

            // The answer keeps the order of the hits and leaves out the ones whose parents the caller cannot read.
            var next = 0;
            foreach (var hit in hits)
            {
                if (next < located.Value!.Count && Same(hit, located.Value[next]))
                {
                    placed[hit] = located.Value[next++];
                }
            }
        }

        var entries = new List<CatalogEntryDto>();
        foreach (var candidate in page)
        {
            if (candidate.Entry is not null)
            {
                entries.Add(candidate.Entry);
            }
            else if (placed.TryGetValue(candidate.Hit!, out var hit))
            {
                entries.Add(ToEntry(candidate.Kind, hit));
            }
        }

        return GenericResult<List<CatalogEntryDto>>.Success(entries);
    }

    private static bool Same(SearchHit hit, SearchHit placed) =>
        hit.Key == placed.Key && hit.ParentKey == placed.ParentKey && string.Equals(hit.Name, placed.Name, StringComparison.Ordinal);

    // Why both: the parents are for a screen that opens the entity, the location is the same names as one line for a person to read.
    private static CatalogEntryDto ToEntry(string kind, SearchHit hit) => new()
    {
        EntityType = kind,
        Name = hit.Name,
        Description = hit.Description,
        Location = hit.Parents.Count == 0 ? null : string.Join(" / ", hit.Parents),
        Parents = hit.Parents.ToList(),
        Owner = hit.Owner,
    };

    // One read per provider, and only for a provider that serves a kind that was asked for.
    private IEnumerable<Func<CancellationToken, Task<IGenericResult<List<CatalogEntryDto>>>>> Reads(CatalogKindSet kinds)
    {
        if (kinds.Wants(CatalogKinds.DataSet))
            yield return ct => DataSetEntries.Read(_dataSets, ct);
        if (kinds.Wants(CatalogKinds.DataStore) || kinds.Wants(CatalogKinds.DataPath) || kinds.Wants(CatalogKinds.Container))
            yield return ct => DataStoreEntries.Read(_dataStores, kinds, ct);
        if (kinds.Wants(CatalogKinds.Calculation))
            yield return ct => CalculationEntries.Read(_calculations, ct);
        if (kinds.Wants(CatalogKinds.Transformation))
            yield return ct => TransformationEntries.Read(_transformations, ct);
        if (kinds.Wants(CatalogKinds.Pipeline))
            yield return ct => PipelineEntries.Read(_pipelines, ct);
    }
}
