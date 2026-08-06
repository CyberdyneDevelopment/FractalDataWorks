#pragma warning disable CS1591
using System.Collections.Generic;
using Fdw.Schema.Indexes;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;

namespace Fdw.Schema.Schemas;

/// <summary>
/// Generic schema definition interface for describing data structures.
/// </summary>
/// <typeparam name="TProperty">The property definition type.</typeparam>
/// <remarks>
/// <para>
/// Provides a unified abstraction for schema metadata across different storage systems
/// (SQL tables, JSON documents, CSV files, etc.).
/// </para>
/// <para>
/// Supports both flat (tabular) and nested (hierarchical) schema definitions through
/// the Layout property and optional Children collection.
/// </para>
/// </remarks>
public interface ISchemaDefinition<TProperty> where TProperty : IPropertyDefinition
{
    /// <summary>
    /// Gets the schema name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the optional description of this schema.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the properties (columns/fields) defined in this schema.
    /// </summary>
    IReadOnlyList<TProperty> Properties { get; }

    /// <summary>
    /// Gets the surrogate key definition (auto-generated, no business meaning).
    /// </summary>
    /// <remarks>
    /// Typically a single-column key (e.g., Id, RowId).
    /// </remarks>
    IKeyDefinition<TProperty>? SurrogateKey { get; }

    /// <summary>
    /// Gets the natural key definition (business identifier, human-meaningful).
    /// </summary>
    /// <remarks>
    /// May be a composite key (e.g., [CountryCode, StateCode] or [FirstName, LastName, BirthDate]).
    /// </remarks>
    IKeyDefinition<TProperty>? NaturalKey { get; }

    /// <summary>
    /// Gets the indexes defined on this schema.
    /// </summary>
    IReadOnlyList<IIndexDefinition<TProperty>> Indexes { get; }

    /// <summary>
    /// Gets the data layout type (Tabular, Hierarchical, Document, KeyValue, Graph).
    /// </summary>
    IDataLayout Layout { get; }

    /// <summary>
    /// Gets the child schemas for hierarchical layouts.
    /// </summary>
    /// <remarks>
    /// Only applicable when Layout.SupportsNesting is true.
    /// For example, a JSON schema for Order might have nested LineItems schema.
    /// </remarks>
    IReadOnlyList<ISchemaDefinition<TProperty>>? Children { get; }

    /// <summary>
    /// Gets the path expression for navigating to this schema within a parent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For hierarchical layouts, specifies how to locate this schema within a parent structure.
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    /// <item>JSON: "$.Orders[*].LineItems"</item>
    /// <item>XML: "/Order/LineItems/LineItem"</item>
    /// <item>Tabular: null (not applicable)</item>
    /// </list>
    /// </para>
    /// </remarks>
    string? PathExpression { get; }

    /// <summary>
    /// Gets a property by name.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The property definition if found; otherwise, null.</returns>
    TProperty? Get(string name);

    /// <summary>
    /// Gets all properties with the specified role.
    /// </summary>
    /// <param name="role">The property role to filter by.</param>
    /// <returns>A list of properties matching the specified role.</returns>
    IReadOnlyList<TProperty> Get(IPropertyRole role);
}
