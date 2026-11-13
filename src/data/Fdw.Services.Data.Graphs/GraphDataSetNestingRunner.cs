using System.Collections.Generic;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Graphs;

/// <summary>
/// Reads a Graph data set that nests other data sets (FDW-5299): refuses a cycle and a nesting past the stated depth, then runs the graph.
/// </summary>
/// <remarks>
/// <para>
/// It wraps the runner that runs the graph and adds the guard in front of it, so the runner and its limits are as they were. A nested Graph data set is read through the gateway, so it
/// comes back through this runner and is checked at its own level; a cycle made while the outer read runs is met by the next level's check, so no read follows a cycle.
/// </para>
/// <para>
/// The cycle check is <see cref="GraphDataSetNesting"/>, over the stored definitions, and its answer for a cycle is the code the check before save answers. The depth is the setting
/// the host states; one that is not a positive number refuses the read by name.
/// </para>
/// </remarks>
public sealed class GraphDataSetNestingRunner : IGraphDataSetRunner
{
    private readonly IGraphDataSetRunner _inner;
    private readonly IGraphDataSetSources _sources;
    private readonly GraphDataSetNestingOptions _options;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="GraphDataSetNestingRunner"/> class.</summary>
    /// <param name="inner">The runner that runs the graph once the nesting is judged.</param>
    /// <param name="sources">Reads the data sets the Graph data set reads, and the ones they read in turn.</param>
    /// <param name="options">How deep a read may nest data sets.</param>
    /// <param name="logger">The logger.</param>
    public GraphDataSetNestingRunner(
        IGraphDataSetRunner inner, IGraphDataSetSources sources, GraphDataSetNestingOptions options, ILogger<GraphDataSetNestingRunner>? logger = null)
    {
        _inner = inner;
        _sources = sources;
        _options = options;
        _logger = logger ?? NullLogger<GraphDataSetNestingRunner>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> Read(
        IDataGateway gateway, IGraphDataSetConfiguration dataSet, CancellationToken cancellationToken = default)
    {
        if (_options.MaxDepth < 1)
        {
            return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetLimitNotStated"), _logger,
                ResultDetails.Create("dataSet", dataSet.Name, "limit", nameof(GraphDataSetNestingOptions.MaxDepth), "value", _options.MaxDepth));
        }

        var nesting = await GraphDataSetNesting.Check(dataSet, _sources, _options.MaxDepth, _logger, cancellationToken).ConfigureAwait(false);
        return nesting.IsFailure
            ? nesting.ToNewResult<IReadOnlyList<IDictionary<string, object?>>>()
            : await _inner.Read(gateway, dataSet, cancellationToken).ConfigureAwait(false);
    }
}
