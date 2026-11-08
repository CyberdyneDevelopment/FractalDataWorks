using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Reads which pipelines write each data set, from the graph pipelines the caller may see (FDW-4488).
/// </summary>
/// <remarks>
/// The pipelines are read flat with their components (<c>GetFlat</c> of the domain provider): the domain's rows, their implementation rows and each child collection once, a
/// fixed number of statements however many pipelines there are. What a graph writes is then worked out in memory by <see cref="GraphPipelineUses"/>, which owns the rule
/// that a component with no outputs writes. Only the components' own columns are needed for that, so the components' stages are left unread. Writers are not transitive:
/// a pipeline that only starts another has a call, not a write, and the called pipeline's runs are tracked under its own name.
/// </remarks>
public sealed class DataSetWriters : IDataSetWriters
{
    private const string Placeholder = "{{";

    private readonly PipelineServiceConfigurationProvider _pipelines;
    private readonly ILogger<DataSetWriters> _logger;

    /// <summary>Initializes a new instance of the <see cref="DataSetWriters"/> class.</summary>
    /// <param name="pipelines">Reads the pipelines.</param>
    /// <param name="logger">The logger the read reports through.</param>
    public DataSetWriters(PipelineServiceConfigurationProvider pipelines, ILogger<DataSetWriters>? logger = null)
    {
        _pipelines = pipelines ?? throw new ArgumentNullException(nameof(pipelines));
        _logger = logger ?? NullLogger<DataSetWriters>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>>>> ByDataSet(CancellationToken cancellationToken = default)
    {
        var read = await _pipelines.GetFlat(ConfigurationLevels.Children, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return read.ToNewResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>>>();

        return GenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>>>.Success(Of(read.Value!.OfType<GraphPipelineConfiguration>(), _logger));
    }

    /// <summary>Works out, from the graphs alone, the writers of each data set.</summary>
    /// <param name="graphs">The graph pipelines, each with its components.</param>
    /// <param name="logger">The logger the writers left out are reported to.</param>
    /// <returns>The data set's name to its writers, in the order the pipelines were given.</returns>
    internal static Dictionary<string, IReadOnlyList<DataSetWriter>> Of(IEnumerable<GraphPipelineConfiguration> graphs, ILogger logger)
    {
        var writers = new Dictionary<string, List<DataSetWriter>>(StringComparer.OrdinalIgnoreCase);
        var placeholders = 0;
        foreach (var graph in graphs)
        {
            var written = GraphPipelineUses.Of(graph)
                .Where(use => string.Equals(use.Relation, PipelineUseRelations.Writes, StringComparison.Ordinal)
                    && string.Equals(use.Kind, PipelineUseKinds.DataSet, StringComparison.Ordinal));
            foreach (var use in written)
            {
                if (use.Name.Contains(Placeholder, StringComparison.Ordinal))
                {
                    placeholders++;
                    continue;
                }

                if (!writers.TryGetValue(use.Name, out var list))
                {
                    list = [];
                    writers[use.Name] = list;
                }

                list.Add(new DataSetWriter(graph.Id, graph.Name, graph.IsEnabled));
            }
        }

        if (placeholders > 0)
            DataSetWriterLog.PlaceholderWriters(logger, placeholders);

        return writers.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<DataSetWriter>)[.. pair.Value], StringComparer.OrdinalIgnoreCase);
    }
}
