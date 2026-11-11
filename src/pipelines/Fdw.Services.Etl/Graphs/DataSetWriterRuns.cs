using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The pipelines that write each data set, each with its latest run (FDW-4488): what the data set domain judges a status by.
/// </summary>
/// <remarks>
/// It answers the data set domain's <see cref="IDataSetWriterRuns"/> from the pipelines side: <see cref="IDataSetWriters"/> says who writes what, and the execution tracker says how
/// each writer's latest run went. Both are read once for the whole request, in a fixed number of statements however many pipelines or data sets there are.
/// </remarks>
public sealed class DataSetWriterRuns : IDataSetWriterRuns
{
    private static readonly IReadOnlyDictionary<string, IExecutionItem> NoRuns = new Dictionary<string, IExecutionItem>(StringComparer.OrdinalIgnoreCase);

    private readonly IDataSetWriters _writers;
    private readonly IExecutionTracker _runs;

    /// <summary>Initializes a new instance of the <see cref="DataSetWriterRuns"/> class.</summary>
    /// <param name="writers">Which pipelines write each data set.</param>
    /// <param name="runs">Where the latest run of each writer is read.</param>
    public DataSetWriterRuns(IDataSetWriters writers, IExecutionTracker runs)
    {
        _writers = writers;
        _runs = runs;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>>>> ByDataSet(CancellationToken cancellationToken = default)
    {
        var written = await _writers.ByDataSet(cancellationToken).ConfigureAwait(false);
        if (written.IsFailure || written.Value is null)
            return written.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>>>();

        var pipelines = written.Value.Values.SelectMany(list => list).Select(writer => writer.PipelineName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var latest = NoRuns;
        if (pipelines.Count > 0)
        {
            var read = await _runs.GetLatestRuns(pipelines, cancellationToken).ConfigureAwait(false);
            if (read.IsFailure || read.Value is null)
                return read.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>>>();

            latest = read.Value;
        }

        var runs = written.Value.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<DataSetWriterRun>)[.. pair.Value.Select(writer => new DataSetWriterRun(writer.PipelineName, writer.IsEnabled, latest.TryGetValue(writer.PipelineName, out var run) ? run : null))],
            StringComparer.OrdinalIgnoreCase);
        return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>>>.Success(runs);
    }
}
