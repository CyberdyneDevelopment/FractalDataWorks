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
using Fdw.Services.Quality;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Finds the named things of the catalog by name or description (FDW-1681): one search over data sets and their fields, data stores
/// with their paths, containers and fields, calculations, transformations, pipelines and glossary terms.
/// </summary>
/// <remarks>
/// Each kind is read from its own provider, so rows are read as the caller and a thing the caller may not see is not found. A read
/// that fails fails the search: a kind is never left out of an answer that claims to have searched it. Matching and paging are done
/// here, over what the providers return, with the case-insensitive comparison the catalog has always used.
/// </remarks>
internal sealed class CatalogSearch
{
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataStoreProvider _dataStores;
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly ICalculationConfigurationProvider _calculations;
    private readonly ITransformationConfigurationProvider _transformations;
    private readonly IGlossaryTermConfigurationProvider _terms;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="CatalogSearch"/> class.</summary>
    /// <param name="dataSets">Supplies the data sets and their fields.</param>
    /// <param name="dataStores">Supplies the data stores with their paths, containers and fields.</param>
    /// <param name="pipelines">Supplies the pipelines.</param>
    /// <param name="calculations">Supplies the calculations.</param>
    /// <param name="transformations">Supplies the transformations.</param>
    /// <param name="terms">Supplies the glossary terms.</param>
    /// <param name="logger">The logger.</param>
    internal CatalogSearch(
        IDataSetConfigurationProvider dataSets,
        IDataStoreProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        ICalculationConfigurationProvider calculations,
        ITransformationConfigurationProvider transformations,
        IGlossaryTermConfigurationProvider terms,
        ILogger? logger)
    {
        _dataSets = dataSets;
        _dataStores = dataStores;
        _pipelines = pipelines;
        _calculations = calculations;
        _transformations = transformations;
        _terms = terms;
        _logger = logger ?? NullLogger.Instance;
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
        var entries = new List<CatalogEntryDto>();
        foreach (var read in Reads(kinds))
        {
            var found = await read(cancellationToken).ConfigureAwait(false);
            if (found.IsFailure)
            {
                return found.ToNewResult<CatalogSearchResponse>();
            }

            entries.AddRange(found.Value!);
        }

        var matches = entries
            .Select(entry => (Entry: entry, Rank: Closeness(entry, text)))
            .Where(match => match.Rank >= 0)
            .OrderBy(match => match.Rank)
            .ThenBy(match => CatalogKinds.IndexOf(match.Entry.EntityType))
            .ThenBy(match => match.Entry.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.Entry.Location, StringComparer.OrdinalIgnoreCase)
            .Select(match => match.Entry)
            .ToList();
        CatalogSearchLog.Searched(_logger, text, kinds.Names.Count, matches.Count);

        return GenericResult<CatalogSearchResponse>.Success(new CatalogSearchResponse
        {
            Items = matches.Skip(skip).Take(take).ToList(),
            Skip = skip,
            Take = take,
            Total = matches.Count,
            HasMore = matches.Count > skip + take,
            Kinds = kinds.Names.ToList(),
        });
    }

    // One read per provider, and only for a provider that serves a kind that was asked for.
    private IEnumerable<Func<CancellationToken, Task<IGenericResult<List<CatalogEntryDto>>>>> Reads(CatalogKindSet kinds)
    {
        if (kinds.Wants(CatalogKinds.DataSet) || kinds.Wants(CatalogKinds.DataSetField))
            yield return ct => DataSetEntries.Read(_dataSets, kinds, ct);
        if (kinds.Wants(CatalogKinds.DataStore) || kinds.Wants(CatalogKinds.DataPath) || kinds.Wants(CatalogKinds.Container) || kinds.Wants(CatalogKinds.ContainerField))
            yield return ct => DataStoreEntries.Read(_dataStores, kinds, ct);
        if (kinds.Wants(CatalogKinds.Calculation))
            yield return ct => CalculationEntries.Read(_calculations, ct);
        if (kinds.Wants(CatalogKinds.Transformation))
            yield return ct => TransformationEntries.Read(_transformations, ct);
        if (kinds.Wants(CatalogKinds.Pipeline))
            yield return ct => PipelineEntries.Read(_pipelines, ct);
        if (kinds.Wants(CatalogKinds.GlossaryTerm))
            yield return ct => GlossaryTermEntries.Read(_terms, ct);
    }

    // How close an entry is to the text: 0 the name is exactly it, 1 the name starts with it, 2 the name contains it, 3 only the
    // description contains it, -1 no match. With no text everything matches equally, so a browse lists by kind and name.
    private static int Closeness(CatalogEntryDto entry, string text)
    {
        if (text.Length == 0)
            return 0;
        if (string.Equals(entry.Name, text, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (entry.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            return 1;
        if (entry.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            return 2;
        return entry.Description is not null && entry.Description.Contains(text, StringComparison.OrdinalIgnoreCase) ? 3 : -1;
    }
}
