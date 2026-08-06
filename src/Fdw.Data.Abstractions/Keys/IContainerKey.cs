using System;
using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A logical or physical key defined on an <see cref="IDataContainer"/>, grouping one or
/// more <see cref="IContainerKeyField"/> entries under a named key relationship.
/// </summary>
/// <remarks>
/// Keys replace the old flat <c>DataContainerKeyField</c> rows and the untyped
/// <c>container.Metadata</c> dictionary. Every key type (Primary, Foreign, Surrogate, Natural,
/// Join) is represented as an <see cref="IContainerKey"/> with a typed <see cref="KeyType"/>
/// that carries behavioral capabilities.
/// <para>
/// Use <see cref="ContainerKeyExtensions.CanEnforceUniqueness"/> to evaluate the derived
/// uniqueness property without requiring default interface implementations
/// (unsupported on <c>netstandard2.0</c> targets).
/// </para>
/// </remarks>
public interface IContainerKey
{
    /// <summary>
    /// Gets the name of this key (e.g., "PK_Connection", "FK_Connection_SecretManager").
    /// </summary>
    string KeyName { get; }

    /// <summary>
    /// Gets an optional description of this key's purpose.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the type of this key, which determines its capabilities and how translators handle it.
    /// </summary>
    KeyTypeBase KeyType { get; }

    /// <summary>
    /// Gets a value indicating whether this key exists as a physical constraint in the database.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> for Join keys, which are metadata-only logical relationships.
    /// </remarks>
    bool IsPhysical { get; }

    /// <summary>
    /// Gets the container referenced by this key, if it is a foreign-key relationship.
    /// </summary>
    /// <value>
    /// The referenced <see cref="IDataContainer"/> for FK keys; <see langword="null"/> for
    /// Primary, Surrogate, Natural, and Join keys that do not reference another container.
    /// </value>
    IDataContainer? ReferencedContainer { get; }

    /// <summary>
    /// Gets the ordered list of fields that participate in this key.
    /// </summary>
    IReadOnlyList<IContainerKeyField> KeyFields { get; }
}
