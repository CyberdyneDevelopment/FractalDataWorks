using System;
using System.Collections.Generic;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The node kinds a lineage request asked for, and the ones it asked for that the lineage does not offer yet.</summary>
internal sealed class LineageKindSelection
{
    /// <summary>The Connection kind: named by the console, not offered by the lineage yet, so a request that names it is told so.</summary>
    public const string Connection = "Connection";

    private static readonly string[] KnownKinds = [MapKinds.DataSet, MapKinds.DataStore, MapKinds.Pipeline, Connection];

    private LineageKindSelection(bool dataSets, bool dataStores, bool pipelines, string[] unavailable)
    {
        DataSets = dataSets;
        DataStores = dataStores;
        Pipelines = pipelines;
        Unavailable = unavailable;
    }

    /// <summary>Gets a value indicating whether data sets are walked.</summary>
    public bool DataSets { get; }

    /// <summary>Gets a value indicating whether data stores are walked.</summary>
    public bool DataStores { get; }

    /// <summary>Gets a value indicating whether pipelines are named on the edges that pass through them.</summary>
    public bool Pipelines { get; }

    /// <summary>Gets the kinds the request named that the lineage does not offer.</summary>
    public string[] Unavailable { get; }

    /// <summary>Whether the kind is one the request asked for.</summary>
    public bool Admits(string kind) => kind switch
    {
        MapKinds.DataSet => DataSets,
        MapKinds.DataStore => DataStores,
        MapKinds.Pipeline => Pipelines,
        _ => false,
    };

    /// <summary>Reads a comma-separated list of kinds, or the endpoint's documented default when none is given.</summary>
    public static IGenericResult<LineageKindSelection> Parse(string? kinds, string defaultKinds, bool pipelinesAreNodes, ILogger logger)
    {
        var chosen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tokens = (string.IsNullOrWhiteSpace(kinds) ? defaultKinds : kinds)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (!Array.Exists(KnownKinds, known => string.Equals(known, token, StringComparison.OrdinalIgnoreCase)))
            {
                return MapRequestRefusals.Invalid<LineageKindSelection>(
                    logger, $"kinds names '{token}', which is not a lineage kind; the kinds are {string.Join(", ", KnownKinds)}");
            }

            chosen.Add(token);
        }

        var selection = new LineageKindSelection(
            chosen.Contains(MapKinds.DataSet),
            chosen.Contains(MapKinds.DataStore),
            chosen.Contains(MapKinds.Pipeline),
            chosen.Contains(Connection) ? [Connection] : []);

        return selection.Walks(pipelinesAreNodes)
            ? GenericResult<LineageKindSelection>.Success(selection)
            : MapRequestRefusals.Invalid<LineageKindSelection>(
                logger, $"kinds names no kind to walk: name DataSet or DataStore{(pipelinesAreNodes ? " or Pipeline" : string.Empty)}");
    }

    private bool Walks(bool pipelinesAreNodes) => DataSets || DataStores || (pipelinesAreNodes && Pipelines);
}
