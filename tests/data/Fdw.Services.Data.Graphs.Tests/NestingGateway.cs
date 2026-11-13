using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Moq;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>
/// A gateway that serves the data sets a test states the way the real one serves them to a graph (FDW-5299): a table gives its rows, and a Graph data set is run by the runner the
/// host registered, through this same gateway, so a nested read comes back through the nesting guard at each level.
/// </summary>
internal sealed class NestingGateway
{
    private readonly Dictionary<string, GraphDataSetConfiguration> _graphs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object?>>> _tables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Mock<IDataGateway> _gateway = new();
    private readonly object _openedLock = new();

    /// <summary>Initializes a new instance of the <see cref="NestingGateway"/> class.</summary>
    public NestingGateway()
    {
        _gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand _, DataSetTarget target, CancellationToken token) => Open(target.DataSet, token));
    }

    /// <summary>Gets the gateway.</summary>
    public IDataGateway Gateway => _gateway.Object;

    /// <summary>Gets the data sets opened through the gateway, in the order they were asked for.</summary>
    public List<string> Opened { get; } = [];

    /// <summary>Gets or sets the runner nested Graph data sets are run by; set once the runner is built over this gateway's data sets.</summary>
    public IGraphDataSetRunner? Runner { get; set; }

    /// <summary>Gets what each stored data set reads, over the data sets this gateway serves.</summary>
    public StubGraphDataSetSources Sources => new StubGraphDataSetSources(_graphs, [.. _tables.Keys]);

    /// <summary>Adds a data set of rows that reads nothing.</summary>
    /// <param name="name">The data set.</param>
    /// <param name="count">How many rows it holds.</param>
    /// <returns>This gateway.</returns>
    public NestingGateway WithTable(string name, int count)
    {
        _tables[name] = [.. Enumerable.Range(0, count).Select(i => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["Id"] = i })];
        return this;
    }

    /// <summary>Adds a Graph data set whose one component reads a data set and whose fields out are that component's output.</summary>
    /// <param name="name">The Graph data set.</param>
    /// <param name="reads">The data set it reads.</param>
    /// <returns>This gateway.</returns>
    public NestingGateway WithGraph(string name, string reads)
    {
        _graphs[name] = Graph(name, reads);
        return this;
    }

    /// <summary>Gets a Graph data set that reads one data set.</summary>
    /// <param name="name">The Graph data set.</param>
    /// <param name="reads">The data set it reads.</param>
    /// <returns>The Graph data set, which is not added to the gateway.</returns>
    public static GraphDataSetConfiguration Graph(string name, string reads)
    {
        var graph = new GraphDataSetConfiguration { Name = name, Implementation = "Graph", FieldsOutComponent = "src", FieldsOutOutput = "output" };
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource", DataSetName = reads, BatchSize = 2 });
        return graph;
    }

    private async Task<IGenericResult<IRecordSource<DataRecord>>> Open(string dataSet, CancellationToken cancellationToken)
    {
        lock (_openedLock)
        {
            Opened.Add(dataSet);
        }

        if (_tables.TryGetValue(dataSet, out var rows))
        {
            return GenericResult<IRecordSource<DataRecord>>.Success(new RowListRecordSource(RowListRecordSource.SchemaOfRows(rows), rows));
        }

        if (!_graphs.TryGetValue(dataSet, out var graph) || Runner is not { } runner)
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(new Fdw.Messages.GenericMessage($"data set '{dataSet}' is not served by this test gateway"));
        }

        var read = await runner.Read(_gateway.Object, graph, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } nested)
        {
            return read.ToNewResult<IRecordSource<DataRecord>>();
        }

        IReadOnlyList<IReadOnlyDictionary<string, object?>> served = [.. nested.Select(row => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(row))];
        return GenericResult<IRecordSource<DataRecord>>.Success(new RowListRecordSource(RowListRecordSource.SchemaOfRows(served), served));
    }
}
