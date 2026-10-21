using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Etl;

/// <summary>
/// A declared container read at another physical path and/or under another name (FDW-1952): the declared container supplies the shape (fields,
/// keys, format, type, operations) and this replaces only the address the connection reads. The connection reads the address from the
/// container's path (the table of a database is in its path), so the path it is given is the declared path moved by its own kind (FDW-1967).
/// </summary>
internal sealed class OverriddenContainer : IDataContainer
{
    private readonly IDataContainer _declared;
    private readonly IPath _path;

    internal OverriddenContainer(IDataContainer declared, IPath movedPath, string? nameOverride)
    {
        _declared = declared;
        _path = movedPath;
        Name = string.IsNullOrWhiteSpace(nameOverride) ? declared.Name : nameOverride;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description => _declared.Description;

    /// <inheritdoc />
    public IReadOnlyList<IDataNode> Nodes => _declared.Nodes;

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name) => _declared.Node(name);

    /// <inheritdoc />
    public IDataNodePath Parent => _declared.Parent;

    /// <inheritdoc />
    public IReadOnlyList<IContainerKey> Keys => _declared.Keys;

    /// <inheritdoc />
    public IGenericResult<IReadOnlyList<ReferencingKeyBinding>> ReferencingKeys => _declared.ReferencingKeys;

    /// <inheritdoc />
    public IContainerType ContainerType => _declared.ContainerType;

    /// <inheritdoc />
    public IFormatType Format => _declared.Format;

    /// <inheritdoc />
    public IContainerSchema Schema => _declared.Schema;

    /// <inheritdoc />
    public IPath Path => _path;

    /// <inheritdoc />
    public string[] SupportedOperations => _declared.SupportedOperations;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Metadata => _declared.Metadata;
}
