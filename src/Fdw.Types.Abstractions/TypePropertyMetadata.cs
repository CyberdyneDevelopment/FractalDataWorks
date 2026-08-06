#pragma warning disable CS1591
namespace Fdw.Types;

/// <summary>
/// Metadata describing a property on a TypeOption for database persistence.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class TypePropertyMetadata
{
    /// <summary>
    /// Name of the property.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full type name of the property (e.g., "System.String", "System.Int32").
    /// </summary>
    public required string PropertyType { get; init; }

    /// <summary>
    /// Property role (if applicable).
    /// </summary>
    public string? PropertyRole { get; init; }

    /// <summary>
    /// SQL type mapping for database persistence.
    /// </summary>
    public string? SqlType { get; init; }

    /// <summary>
    /// Max length for string properties.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Whether the property can be null.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Whether the property is a collection type.
    /// </summary>
    public bool IsCollection { get; init; }
}
