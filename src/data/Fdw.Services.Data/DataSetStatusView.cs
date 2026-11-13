using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// The status of each data set in one request, worked out from what was read once (FDW-4488).
/// </summary>
internal sealed class DataSetStatusView : IDataSetStatusView
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>> _runs;
    private readonly TimeSpan _window;
    private readonly DateTimeOffset _now;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly ILogger _logger;

    // What each data set the walk met reads, for the kinds that hold no data of their own; read once per request however many Graph data sets reach it.
    private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _nests = new(StringComparer.OrdinalIgnoreCase);

    internal DataSetStatusView(
        IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>> runs, TimeSpan window, DateTimeOffset now, IDataSetConfigurationProvider dataSets, ILogger logger)
    {
        _runs = runs;
        _window = window;
        _now = now;
        _dataSets = dataSets;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IGenericResult<IDataSetStatus?> StatusOf(IDataSetImplementationConfiguration dataSet)
    {
        // A variation holds no data of its own (it runs the data set it points at), so no pipeline writes it and it has no status of its own.
        if (dataSet is IDataSetVariationImplementationConfiguration)
            return GenericResult<IDataSetStatus?>.Success(null);

        if (dataSet is IGraphDataSetConfiguration graph)
            return StatusOfGraph(graph);

        var resolved = DataSetStatusResolver.Resolve(
            dataSet.Name, new DataSetStatusInput(_runs.TryGetValue(dataSet.Name, out var found) ? found : [], _window, _now), _logger);
        return resolved.IsFailure || resolved.Value is null
            ? resolved.ToNewResult<IDataSetStatus?>()
            : GenericResult<IDataSetStatus?>.Success(resolved.Value);
    }

    // A Graph data set is read live and no pipeline writes it, so "nothing writes it" is not true of it and says nothing about it (FDW-5296). Its status is that of the pipelines
    // that write the data sets it reads, by the rules every other data set's status follows; with none, the status its kind states. What it only intends to read feeds nothing
    // yet. This answers from what the Graph already holds, so a data set it reads through a nested Graph is not followed here: StatusOfNested follows the nest (FDW-5299).
    private IGenericResult<IDataSetStatus?> StatusOfGraph(IGraphDataSetConfiguration graph)
    {
        var writers = GraphDataSetReads.Of(graph)
            .Where(read => !read.IsIntended && string.Equals(read.Kind, GraphDataSetReadKinds.DataSet, StringComparison.Ordinal))
            .SelectMany(read => _runs.TryGetValue(read.Name, out var found) ? found : [])
            .Distinct()
            .ToList();
        return StatusFromWriters(graph.Name, writers);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IDataSetStatus?>> StatusOfNested(IDataSetImplementationConfiguration dataSet, CancellationToken cancellationToken = default)
    {
        if (dataSet is not IGraphDataSetConfiguration)
            return StatusOf(dataSet);

        var writers = await NestedWriters(dataSet.Name, cancellationToken).ConfigureAwait(false);
        return writers.IsFailure || writers.Value is null
            ? writers.ToNewResult<IDataSetStatus?>()
            : StatusFromWriters(dataSet.Name, writers.Value);
    }

    // A Graph that reaches no data set a pipeline writes is stated NotApplicable; with writers it resolves by the rules every other data set's status follows.
    private IGenericResult<IDataSetStatus?> StatusFromWriters(string dataSet, List<DataSetWriterRun> writers)
    {
        if (writers.Count == 0)
            return GenericResult<IDataSetStatus?>.Success(DataSetStatuses.NotApplicable);

        var resolved = DataSetStatusResolver.Resolve(dataSet, new DataSetStatusInput(writers, _window, _now), _logger);
        return resolved.IsFailure || resolved.Value is null
            ? resolved.ToNewResult<IDataSetStatus?>()
            : GenericResult<IDataSetStatus?>.Success(resolved.Value);
    }

    // The writers of every data set a Graph reaches, level by level: a data set a pipeline writes ends the walk there; one that holds no data (a Graph or a Variation) is read once
    // and what it reads is walked in turn. Every name is met once, so a cycle through them ends instead of being followed.
    private async Task<IGenericResult<List<DataSetWriterRun>>> NestedWriters(string graph, CancellationToken cancellationToken)
    {
        var writers = new List<DataSetWriterRun>();
        var met = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { graph };
        var level = await Nest(graph, cancellationToken).ConfigureAwait(false);
        while (level.IsSuccess && level.Value is { Count: > 0 } names)
        {
            var next = new List<string>();
            foreach (var name in names)
            {
                if (!met.Add(name))
                    continue;

                if (_runs.TryGetValue(name, out var found))
                {
                    writers.AddRange(found);
                    continue;
                }

                var reads = await Nest(name, cancellationToken).ConfigureAwait(false);
                if (reads.IsFailure || reads.Value is null)
                    return reads.ToNewResult<List<DataSetWriterRun>>();

                next.AddRange(reads.Value);
            }

            level = GenericResult<IReadOnlyList<string>>.Success(next);
        }

        return level.IsFailure
            ? level.ToNewResult<List<DataSetWriterRun>>()
            : GenericResult<List<DataSetWriterRun>>.Success([.. writers.Distinct()]);
    }

    // What a data set reads, when it is a kind that holds no data of its own; nothing for a kind that does, and for a data set that does not exist.
    private async Task<IGenericResult<IReadOnlyList<string>>> Nest(string name, CancellationToken cancellationToken)
    {
        if (_nests.TryGetValue(name, out var known))
            return GenericResult<IReadOnlyList<string>>.Success(known);

        var read = await GraphDataSetDefinitions.Read(_dataSets, name, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return read.ToNewResult<IReadOnlyList<string>>();

        List<string> reads = read.Value is IGraphDataSetConfiguration or IDataSetVariationImplementationConfiguration
            ? GraphDataSetNestedReads.Of(read.Value)
            : [];
        _nests[name] = reads;
        return GenericResult<IReadOnlyList<string>>.Success(reads);
    }
}
