using System;
using Fdw.Data.Abstractions;
// Why: Use alias to avoid IDataField ambiguity — both Fdw.Data.Abstractions and
// Fdw.Data.DataSets.Abstractions define IDataField with different contracts.
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
        // Why: Each source wraps its config as a DataSetSourceNode so the IDataSet graph can
        // navigate source identity without coupling to the configuration layer.
        Node = new DataSetSourceNode(config);
        // Why: No alias in configuration — alias is a query-time concept used in JOIN ON clauses.
        // When join support is needed, a separate JoinAlias property can be added to the config.
        Alias = null;
        // Why: Source-level pre-filters are not stored in DataSetSourceConfiguration today.
        // The Filters property on DataSetConfiguration carries dataset-level filters; source-level
        // filters will be added in a future phase when the query optimizer needs per-source pushdown.
        Filter = null;
    }

    /// <inheritdoc />
    public IDataNode Node { get; }

    /// <inheritdoc />
    public string? Alias { get; }

    /// <inheritdoc />
    public IFilterExpression? Filter { get; }
}
