using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;
// Why: DataFieldConfiguration lives in Fdw.Data.DataSets.Abstractions namespace
// but Data.DataSets.Abstractions also defines IDataField (DataSets domain). To avoid ambiguity
// with Data.Abstractions.IDataField (IDataNode hierarchy — the one we implement here),
// use the fully-qualified name for DataFieldConfiguration.
using DataFieldConfiguration = Fdw.Data.DataSets.Abstractions.DataFieldConfiguration;

namespace Fdw.Services.Data.Runtime;

/// <summary>
/// Runtime implementation of <see cref="IDataField"/> built from a <see cref="DataFieldConfiguration"/> record.
/// </summary>
internal sealed class DataSetRuntimeField : IDataField
{
    /// <summary>
    /// Initializes a new instance of <see cref="DataSetRuntimeField"/> from a field configuration record.
    /// </summary>
    /// <param name="config">The field configuration record.</param>
    public DataSetRuntimeField(DataFieldConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        Name = config.Name;
        Description = config.Description;
        // Why: ExplicitType and Binding are not populated from configuration — the configuration
        // carries TypeName as a plain string, not a resolved IDataType. Full type resolution
        // requires a type registry not available at this layer. The field is in Described state
        // (Name present, ExplicitType null) until a higher-level component resolves the type.
        ExplicitType = null;
        Binding = null;
        Ordinal = config.Ordinal;
        IsNullable = !config.IsRequired;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IDataType? ExplicitType { get; }

    /// <inheritdoc />
    public IFieldBinding? Binding { get; }

    /// <inheritdoc />
    public int Ordinal { get; }

    /// <inheritdoc />
    public bool IsNullable { get; }

    /// <inheritdoc />
    // Why: a field is a leaf IDataNode — it has no children.
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <inheritdoc />
    // Why: a leaf field never has child nodes, so Node(name) always fails (no Try*, no nullable).
    public IGenericResult<IDataNode> Node(string name) =>
        GenericResult<IDataNode>.Failure(
            DataStoreLoaderLog.LeafFieldHasNoChild(NullLogger.Instance, Name, name));
}
