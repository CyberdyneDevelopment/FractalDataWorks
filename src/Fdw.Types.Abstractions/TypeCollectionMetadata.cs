#pragma warning disable CS1591
using System.Collections.Generic;

namespace Fdw.Types;

/// <summary>
/// Metadata describing a TypeCollection for database persistence.
/// </summary>
public sealed class TypeCollectionMetadata
{
    /// <summary>
    /// Unique identifier computed from FullName (FNV-1a hash).
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Simple name of the collection (e.g., "FilterOperators").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full namespace-qualified name (e.g., "Fdw.Data.FilterOperators").
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// The kind of collection (Immutable, Mutable, Service, etc.).
    /// </summary>
    public required ICollectionKind CollectionKind { get; init; }

    /// <summary>
    /// For ServiceTypeCollections, the service category (e.g., "Connection", "Authentication").
    /// </summary>
    public string? ServiceCategory { get; init; }

    /// <summary>
    /// Assembly-qualified name of the collection type.
    /// </summary>
    public string? AssemblyQualifiedName { get; init; }

    /// <summary>
    /// TypeOptions belonging to this collection.
    /// </summary>
    public IReadOnlyList<TypeOptionMetadata> Options { get; init; } = [];
}
