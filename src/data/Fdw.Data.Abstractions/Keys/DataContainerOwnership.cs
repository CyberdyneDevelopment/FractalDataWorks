using System;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Reads, from the keys a container declares, whether its rows hang from a row of another container: an implementation body, a typed
/// child list or a key/value property collection. Such a container holds part of a configuration, never a configuration of its own.
/// </summary>
/// <remarks>
/// A key holds the owner when it references another container and is either declared the owner key (<see cref="IContainerKey.IsOwner"/>,
/// FDW-4240) or references that container's Physical key, so each row belongs to one version of one parent row. A Foreign key to another
/// container's Logical key that is not declared the owner cites that record (a tag, a threshold, a parent node) and does not hold the row.
/// This is the rule the configuration write path uses to find the key a child hangs from; reads that must tell a configuration from its
/// parts use the same rule (FDW-4644). Nothing is read from a container's name.
/// </remarks>
public static class DataContainerOwnership
{
    /// <summary>Whether any key the container declares holds its rows under a row of another container.</summary>
    /// <param name="container">The container to inspect.</param>
    /// <returns><see langword="true"/> when the container is a part of another container's configuration.</returns>
    public static bool IsOwnedByAnotherContainer(this IDataContainer container)
    {
        if (container is null) throw new ArgumentNullException(nameof(container));

        for (var i = 0; i < container.Keys.Count; i++)
        {
            if (container.Keys[i].HoldsOwner())
                return true;
        }

        return false;
    }

    /// <summary>Whether the key holds the rows of its container under a row of the container it references.</summary>
    /// <param name="key">The key to inspect.</param>
    /// <returns><see langword="true"/> for a declared owner key, or a key that references the other container's Physical key.</returns>
    public static bool HoldsOwner(this IContainerKey key)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));

        return key.ReferencedContainer is { } referenced && key.HoldsOwnerUnder(referenced);
    }

    /// <summary>Whether the key holds the rows of its container under rows of <paramref name="parent"/>.</summary>
    /// <param name="key">A key of the child container that references <paramref name="parent"/>.</param>
    /// <param name="parent">The container the key references.</param>
    /// <returns><see langword="true"/> for a key declared the owner, or a key whose first field references the parent's Physical key.</returns>
    /// <remarks>
    /// The one rule for both sides: reads ask it of each key's referenced container (<see cref="HoldsOwner"/>), and the configuration write path
    /// asks it of the parent whose referencing keys it is walking.
    /// </remarks>
    public static bool HoldsOwnerUnder(this IContainerKey key, IDataContainer parent)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (parent is null) throw new ArgumentNullException(nameof(parent));

        if (key.KeyFields.Count == 0)
            return false;

        if (key.IsOwner)
            return true;

        return key.KeyFields[0].ReferencedField is { } referencedField
            && parent.PhysicalKeyField() is { } physical
            && string.Equals(referencedField.Name, physical, StringComparison.Ordinal);
    }

    /// <summary>The first field of the Physical key that identifies the container's own rows.</summary>
    /// <param name="container">The container to inspect.</param>
    /// <returns>The field's name, or <see langword="null"/> when the container declares no Physical key of its own.</returns>
    /// <remarks>A Physical key that references another container is a physical Foreign key, not this container's identity.</remarks>
    public static string? PhysicalKeyField(this IDataContainer container)
    {
        if (container is null) throw new ArgumentNullException(nameof(container));

        for (var i = 0; i < container.Keys.Count; i++)
        {
            var key = container.Keys[i];
            if (key.KeyType is PhysicalKeyType && key.ReferencedContainer is null && key.KeyFields.Count > 0)
                return key.KeyFields[0].LocalField.Name;
        }

        return null;
    }
}
