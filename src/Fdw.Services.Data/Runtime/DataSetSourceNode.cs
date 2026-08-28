using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
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
        Name = config.SourceName;
        Description = null;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name) =>
        GenericResult<IDataNode>.Failure(
            DataNodeTreeLog.LeafFieldHasNoChild(NullLogger.Instance, Name, name));
}
