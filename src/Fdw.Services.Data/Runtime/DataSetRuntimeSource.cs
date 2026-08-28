using System;
using Fdw.Data.Abstractions;
using DataSetSourceConfiguration = Fdw.Data.DataSets.Abstractions.DataSetSourceConfiguration;

namespace Fdw.Services.Data.Runtime;

/// <summary>
/// Runtime implementation of <see cref="IDataSetSource"/> built from a <see cref="DataSetSourceConfiguration"/> record.
/// </summary>
internal sealed class DataSetRuntimeSource : IDataSetSource
{
    /// <summary>
    /// Initializes a new <see cref="DataSetRuntimeSource"/> from a source configuration record.
    /// </summary>
    /// <param name="config">The source configuration record.</param>
    public DataSetRuntimeSource(DataSetSourceConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Node = new DataSetSourceNode(config);
        Alias = null;
        Filter = null;
    }

    /// <inheritdoc />
    public IDataNode Node { get; }

    /// <inheritdoc />
    public string? Alias { get; }

    /// <inheritdoc />
    public IFilterExpression? Filter { get; }
}
