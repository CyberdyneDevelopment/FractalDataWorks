using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// Runtime implementation of <see cref="IContainerKey"/>.
/// Groups one or more <see cref="IContainerKeyField"/> entries under a named key type.
/// Constructed by the per-transport <c>DataStoreBuilderBase</c> from <c>data.DataContainerKey</c> +
/// <c>data.DataContainerKeyField</c> rows, and by <c>MsSqlDataContainerDetailLoader</c>
/// for lazy-loaded containers.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ContainerKey : IContainerKey
{
    /// <inheritdoc />
    public string KeyName { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public KeyTypeBase KeyType { get; }

    /// <inheritdoc />
    public bool IsPhysical { get; }

    /// <inheritdoc />
    public IDataContainer? ReferencedContainer { get; }

    /// <inheritdoc />
    public IReadOnlyList<IContainerKeyField> KeyFields { get; }

    /// <summary>Initializes a new instance of the <see cref="ContainerKey"/> class.</summary>
    public ContainerKey(
        string keyName,
        string? description,
        KeyTypeBase keyType,
        bool isPhysical,
        IDataContainer? referencedContainer,
        IReadOnlyList<IContainerKeyField> keyFields)
    {
        KeyName = keyName;
        Description = description;
        KeyType = keyType;
        IsPhysical = isPhysical;
        ReferencedContainer = referencedContainer;
        KeyFields = keyFields;
    }
}
