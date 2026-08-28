using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// Generic runtime implementation of <see cref="IDataField"/> — a leaf <see cref="IDataNode"/>.
/// </summary>
/// <remarks>
/// Why public: shared across transport-specific builders in OTHER assemblies (e.g.
/// <c>Fdw.Services.Connections.FileSystem</c>'s <c>FileSystemDataStoreBuilder</c>, mirroring
/// <c>Fdw.Services.Connections.MsSql</c>'s own <c>MsSqlDataField</c>) — genuinely generic, not a detail
/// private to this assembly.
/// </remarks>
public sealed class DataField : IDataField
{
    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IDataType? ExplicitType { get; }

    /// <inheritdoc />
    public IFieldBinding? Binding => null;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="DataField"/> class.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="description">Optional human-readable description.</param>
    /// <param name="explicitType">The explicitly declared abstract type, if any; <see langword="null"/> for generic transports with no native-type system.</param>
    /// <param name="ordinal">The field's declared ordinal position.</param>
    /// <param name="isNullable">Whether the field is declared nullable.</param>
    public DataField(string name, string? description, IDataType? explicitType, int ordinal, bool isNullable)
    {
        Name = name;
        Description = description;
        ExplicitType = explicitType;
        Ordinal = ordinal;
        IsNullable = isNullable;
    }
}
