using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name) =>
        GenericResult<IDataNode>.Failure(
            DataStoreLoaderLog.LeafFieldHasNoChild(NullLogger.Instance, Name, name));
}
