using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A declared container read or written at another physical path and/or under another name (FDW-1952, FDW-3193): the declared container supplies
/// the shape (fields, keys, format, type, operations) and this replaces only the address the connection uses. The connection reads the address
/// from the container's path (the table of a database is in its path), so the path it is given is the declared path moved by its own kind
/// (FDW-1967). The ETL data gateway uses it to read a file and a load uses it to write one, so there is one way a container is moved.
/// </summary>
public sealed class ReaddressedContainer : IDataContainer
{
    private readonly IDataContainer _declared;
    private readonly IPath _path;

    /// <summary>Initializes a new instance of the <see cref="ReaddressedContainer"/> class.</summary>
    /// <param name="declared">The declared container, which supplies the shape.</param>
    /// <param name="movedPath">The path the connection reads or writes in place of the declared one.</param>
    /// <param name="nameOverride">The name to use in place of the declared one; none or blank keeps it.</param>
    public ReaddressedContainer(IDataContainer declared, IPath movedPath, string? nameOverride)
    {
        _declared = declared;
        _path = movedPath;
        Name = nameOverride is { } named && !string.IsNullOrWhiteSpace(named) ? named : declared.Name;
    }

    /// <summary>Makes the declared container again at another path and/or name, when its path can be moved.</summary>
    /// <param name="declared">The declared container.</param>
    /// <param name="pathOverride">The path part to use in place of the declared one; none or blank keeps it.</param>
    /// <param name="nameOverride">The name to use in place of the declared one; none or blank keeps it.</param>
    /// <returns>The moved container, or <see langword="null"/> when the declared container has no path or a path of a kind that cannot be moved.</returns>
    public static IDataContainer? TryMake(IDataContainer declared, string? pathOverride, string? nameOverride)
        => declared.Path is IReaddressablePath movable
            ? new ReaddressedContainer(declared, movable.Readdress(pathOverride, nameOverride), nameOverride)
            : null;

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
