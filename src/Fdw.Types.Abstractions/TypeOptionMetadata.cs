#pragma warning disable CS1591
using System.Collections.Generic;

namespace Fdw.Types;

/// <summary>
/// Metadata describing a single TypeOption for database persistence.
/// </summary>
public sealed class TypeOptionMetadata
{
    /// <summary>
    /// The integer ID of the TypeOption within its collection.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Name of the TypeOption (e.g., "Equal", "Contains").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Parent TypeCollection ID.
    /// </summary>
    public required int TypeCollectionId { get; init; }

    /// <summary>
    /// Full type name of the TypeOption class.
    /// </summary>
    public required string FullTypeName { get; init; }

    /// <summary>
    /// Optional category for grouping related options.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Optional description of this TypeOption.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Properties defined on this TypeOption.
    /// </summary>
    public IReadOnlyList<TypePropertyMetadata> Properties { get; init; } = [];
}
