using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;

namespace Fdw.Services.Data;

/// <summary>
/// The Graph kind of data set (FDW-5286): a data set defined by a graph of row components that ends in its fields out, read by running
/// the graph and storing nothing.
/// </summary>
/// <remarks>
/// A read runs the graph through the runner the host registered (<see cref="IGraphDataSetRunner"/>, reached through the execution context) and
/// answers the rows of the fields out, with the data set's filter and the caller's filter, ordering and page applied to them. A graph that cannot
/// finish, or that has no stored body, answers a named failure and never the rows read so far. It states that it pages and aggregates nowhere in
/// a store, as Federated does. The kind is stateless and holds no providers, as the other kinds do.
/// </remarks>
[TypeOption(typeof(DataSetTypes), "Graph")]
public sealed class GraphDataSetType : DataSetTypeBase, IShapesItsOwnRead, IKeepsItsOwnFields
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetType"/> class.</summary>
    public GraphDataSetType()
        : base("Graph", "A data set defined by a graph of row components, read by running the graph",
            typeof(object), Array.Empty<IDataField>(), category: "DataSetStrategy")
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The graph runs whole and <see cref="GraphReadShape"/> applies the filter, the ordering and the page to the rows it holds, so a page asked for
    /// is the page returned. <see cref="DataSetTypeBase.PagesInStore"/> stays false: no store pages it, so a read that makes this a source of
    /// another read takes it whole (FDW-5300).
    /// </remarks>
    public bool ShapesItsOwnRead => true;

    /// <inheritdoc/>
    public bool FieldsHaveHistory => false;

    /// <inheritdoc/>
    public bool FieldsTakeStandIns => false;

    /// <inheritdoc/>
    public IReadOnlyList<DataSetFieldConfiguration> FieldsOf(IDataSetImplementationConfiguration dataSet) => [.. dataSet.Fields];

    /// <inheritdoc/>
    public void KeepFields(IDataSetImplementationConfiguration dataSet, IReadOnlyList<DataSetFieldConfiguration> fields) => dataSet.Fields = [.. fields];

    /// <inheritdoc/>
    public override IDataQuery CreateQuery() => new DataQueryBuilder<object>(Name);

    /// <inheritdoc/>
    public override async Task<IGenericResult<T>> Execute<T>(
        IDataSetExecutionContext context, IDataCommand command, CancellationToken ct = default)
    {
        if (context is not DataSetExecutionContext ctx)
            return GenericResult<T>.Failure(DataServiceResultCodes.ByName("DataSetConfigurationRequired"));

        if (ctx.Config is not IGraphDataSetConfiguration graph)
            return DataSetExecutionHelpers.KindMismatch<T>(ctx, "Graph");

        // Why the abstract data set is answered before the run: a graph nothing feeds has no rows to read, and the run would only fail on it (FDW-5294).
        if (!graph.IsBound())
            return GenericResult<T>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetNotBound"), ctx.Logger,
                ResultDetails.Create("dataSet", graph.Name));

        // Why a host with no runner is refused by name: it is miswired, and an empty answer would read as a data set with no rows.
        if (ctx.GraphRunner is not { } runner || ctx.Gateway is not { } gateway)
            return GenericResult<T>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetRunnerMissing"), ctx.Logger,
                ResultDetails.Create("dataSet", graph.Name));

        var filter = DataSetReadFilter.For(ctx, command);
        if (filter.IsFailure)
            return filter.ToNewResult<T>();

        var read = await runner.Read(gateway, graph, ct).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } rows)
            return read.ToNewResult<T>();

        return DataSetExecutionHelpers.ConvertDictionariesToType<T>(ctx, GraphReadShape.Apply(rows, filter.Value, command));
    }
}
