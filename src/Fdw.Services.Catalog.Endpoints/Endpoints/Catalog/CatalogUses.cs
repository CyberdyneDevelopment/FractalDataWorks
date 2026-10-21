using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Quality;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Answers where a named thing is used and what it uses (FDW-1683), from the settings of the pipelines, calculations, glossary terms
/// and data sets themselves: nothing is typed in twice.
/// </summary>
/// <remarks>
/// Each provider is read as the caller, so a thing the caller may not see is absent from the answer. A read that fails fails the question;
/// a kind is never left out of an answer that claims to have read it. A thing nothing uses answers an empty list.
/// </remarks>
internal sealed class CatalogUses
{
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly ICalculationConfigurationProvider _calculations;
    private readonly ITransformationConfigurationProvider _transformations;
    private readonly IGlossaryTermConfigurationProvider _terms;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="CatalogUses"/> class.</summary>
    /// <param name="dataSets">Supplies the data sets and where they come from.</param>
    /// <param name="pipelines">Supplies the pipelines.</param>
    /// <param name="calculations">Supplies the calculations.</param>
    /// <param name="transformations">Supplies the transformations, so a tool's transformation id has a name.</param>
    /// <param name="terms">Supplies the glossary terms.</param>
    /// <param name="logger">The logger.</param>
    internal CatalogUses(
        IDataSetConfigurationProvider dataSets,
        IPipelineConfigurationProvider pipelines,
        ICalculationConfigurationProvider calculations,
        ITransformationConfigurationProvider transformations,
        IGlossaryTermConfigurationProvider terms,
        ILogger? logger)
    {
        _dataSets = dataSets;
        _pipelines = pipelines;
        _calculations = calculations;
        _transformations = transformations;
        _terms = terms;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Answers one question whose request has already been checked.</summary>
    /// <param name="kind">The kind of the thing, one of <see cref="CatalogKinds"/>.</param>
    /// <param name="name">The thing's name.</param>
    /// <param name="parents">The names it sits under; empty to match any thing of that name.</param>
    /// <param name="direction">One of <see cref="CatalogUsesDirections"/>.</param>
    /// <param name="cancellationToken">Cancels the question.</param>
    /// <returns>The answer, or the failure of the first read that could not be answered.</returns>
    internal async Task<IGenericResult<CatalogUsesResponse>> Run(
        string kind, string name, IReadOnlyList<string> parents, string direction, CancellationToken cancellationToken)
    {
        var edges = new List<CatalogEdge>();
        foreach (var read in Reads(direction, kind))
        {
            var found = await read(cancellationToken).ConfigureAwait(false);
            if (found.IsFailure)
            {
                return found.ToNewResult<CatalogUsesResponse>();
            }

            edges.AddRange(found.Value!);
        }

        var items = string.Equals(direction, CatalogUsesDirections.UsedBy, StringComparison.Ordinal)
            ? edges.Where(edge => Names(edge.ToKind, edge.ToName, edge.ToParents, kind, name, parents))
                .Select(edge => new CatalogUseDto { Relation = edge.Relation, EntityType = edge.FromKind, Name = edge.FromName })
            : edges.Where(edge => Names(edge.FromKind, edge.FromName, [], kind, name, []))
                .Select(edge => new CatalogUseDto { Relation = edge.Relation, EntityType = edge.ToKind, Name = edge.ToName, Parents = [.. edge.ToParents] });
        var ordered = items
            .OrderBy(item => CatalogKinds.IndexOf(item.EntityType))
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Relation, StringComparer.Ordinal)
            .ToList();
        CatalogSearchLog.UsesAnswered(_logger, kind, name, direction, ordered.Count);

        return GenericResult<CatalogUsesResponse>.Success(new CatalogUsesResponse
        {
            Kind = kind,
            Name = name,
            Direction = direction,
            Items = ordered,
            Total = ordered.Count,
        });
    }

    // Which readings the question needs. What uses a thing can be any of them; what a thing uses is read from that kind alone.
    private IEnumerable<Func<CancellationToken, Task<IGenericResult<List<CatalogEdge>>>>> Reads(string direction, string kind)
    {
        var usedBy = string.Equals(direction, CatalogUsesDirections.UsedBy, StringComparison.Ordinal);
        if (usedBy || string.Equals(kind, CatalogKinds.Pipeline, StringComparison.Ordinal))
            yield return ct => PipelineEdges.Read(_pipelines, _transformations, ct);
        if (usedBy || string.Equals(kind, CatalogKinds.Calculation, StringComparison.Ordinal))
            yield return ct => CalculationEdges.Read(_calculations, ct);
        if (usedBy || string.Equals(kind, CatalogKinds.GlossaryTerm, StringComparison.Ordinal))
            yield return ct => GlossaryTermEdges.Read(_terms, ct);
        if (usedBy || string.Equals(kind, CatalogKinds.DataSet, StringComparison.Ordinal))
            yield return ct => DataSetEdges.Read(_dataSets, ct);
    }

    // Whether the end of an edge is the thing asked about: same kind, same name, and the parents the question stated.
    private static bool Names(
        string edgeKind, string edgeName, IReadOnlyList<string> edgeParents, string kind, string name, IReadOnlyList<string> parents) =>
        string.Equals(edgeKind, kind, StringComparison.Ordinal)
        && string.Equals(edgeName, name, StringComparison.OrdinalIgnoreCase)
        && (parents.Count == 0 || (edgeParents.Count == parents.Count
            && edgeParents.Zip(parents, (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase)).All(same => same)));
}
