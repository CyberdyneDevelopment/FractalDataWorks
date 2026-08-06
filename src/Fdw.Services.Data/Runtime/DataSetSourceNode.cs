using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
// Why: DataSetSourceConfiguration is in Fdw.Data.DataSets.Abstractions. Using an alias
// avoids ambiguity with Fdw.Data.Abstractions.IDataField — both namespaces define
// IDataField with different contracts; the one we implement is from Data.Abstractions (IDataNode hierarchy).
using DataSetSourceConfiguration = Fdw.Data.DataSets.Abstractions.DataSetSourceConfiguration;

namespace Fdw.Services.Data.Runtime;

/// <summary>
/// Runtime <see cref="IDataNode"/> that wraps a <see cref="DataSetSourceConfiguration"/> record
/// for use as the node backing an <see cref="IDataSetSource"/>.
/// </summary>
/// <remarks>
/// Fields are always empty at this level: sources refer to physical containers whose field
/// metadata lives in the DataStore tree, not in the DataSet source configuration record.
/// </remarks>
internal sealed class DataSetSourceNode : IDataNode
{
    /// <summary>
    /// Initializes a new <see cref="DataSetSourceNode"/> from the given source configuration.
    /// </summary>
    /// <param name="config">The source configuration record.</param>
    public DataSetSourceNode(DataSetSourceConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        // Why: SourceName identifies the source within the DataSet; DataStoreName/ConnectionName
        // are routing hints at the connection layer, not the node's identity in the graph.
        Name = config.SourceName;
        Description = null;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    // Why: a source node has no child nodes in the graph — the physical container's fields live in
    // the DataStore tree, resolved by DataGatewayService at query time, not as children here.
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <inheritdoc />
    // Why: no children, so Node(name) always fails (no Try*, no nullable).
    public IGenericResult<IDataNode> Node(string name) =>
        GenericResult<IDataNode>.Failure(
            DataNodeTreeLog.LeafFieldHasNoChild(NullLogger.Instance, Name, name));
}
