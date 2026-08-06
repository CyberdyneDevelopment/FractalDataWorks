#pragma warning disable CS1591
using System.Collections.Generic;

namespace Fdw.Schema.Properties;

/// <summary>
/// Base interface for all property/field/column definitions in a schema.
/// </summary>
/// <remarks>
/// This interface provides the core metadata for any property-like element
/// across different storage systems (SQL columns, JSON fields, CSV columns, etc.).
/// </remarks>
public interface IPropertyDefinition
{
    /// <summary>
    /// Gets the property name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the property role (e.g., SurrogateKey, NaturalKey, Attribute, Measure, Lookup).
    /// </summary>
    IPropertyRole Role { get; }

    /// <summary>
    /// Gets a value indicating whether this property is required (NOT NULL).
    /// </summary>
    bool IsRequired { get; }

    /// <summary>
    /// Gets an optional description of this property.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets optional metadata associated with this property.
    /// </summary>
    /// <remarks>
    /// Used for provider-specific metadata, documentation, or annotations.
    /// </remarks>
    IReadOnlyDictionary<string, object>? Metadata { get; }
}
