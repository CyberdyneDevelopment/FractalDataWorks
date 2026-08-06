#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Schema.Indexes;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;

namespace Fdw.Schema.Schemas;

/// <summary>
/// Abstract base class for schema definitions.
/// </summary>
/// <typeparam name="TProperty">The property definition type.</typeparam>
/// <remarks>
/// <para>
/// Provides default implementations for common schema operations like property lookup
/// and role-based filtering.
/// </para>
/// <para>
/// Derived classes typically only need to override the property initializers.
/// </para>
/// </remarks>
public abstract class SchemaDefinitionBase<TProperty> : ISchemaDefinition<TProperty>
    where TProperty : IPropertyDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaDefinitionBase{TProperty}"/> class.
    /// </summary>
    /// <param name="name">The schema name.</param>
    /// <param name="properties">The properties defined in this schema.</param>
    /// <param name="layout">The data layout type.</param>
    /// <param name="description">The optional description.</param>
    /// <param name="surrogateKey">The optional surrogate key definition.</param>
    /// <param name="naturalKey">The optional natural key definition.</param>
    /// <param name="indexes">The optional indexes.</param>
    /// <param name="children">The optional child schemas for hierarchical layouts.</param>
    /// <param name="pathExpression">The optional path expression for nested schemas.</param>
    protected SchemaDefinitionBase(
        string name,
        IReadOnlyList<TProperty> properties,
        IDataLayout layout,
        string? description = null,
        IKeyDefinition<TProperty>? surrogateKey = null,
        IKeyDefinition<TProperty>? naturalKey = null,
        IReadOnlyList<IIndexDefinition<TProperty>>? indexes = null,
        IReadOnlyList<ISchemaDefinition<TProperty>>? children = null,
        string? pathExpression = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Schema name cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Description = description;
        SurrogateKey = surrogateKey;
        NaturalKey = naturalKey;
        Indexes = indexes ?? Array.Empty<IIndexDefinition<TProperty>>();
        Children = children;
        PathExpression = pathExpression;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string? Description { get; }

    /// <inheritdoc/>
    public IReadOnlyList<TProperty> Properties { get; }

    /// <inheritdoc/>
    public IKeyDefinition<TProperty>? SurrogateKey { get; }

    /// <inheritdoc/>
    public IKeyDefinition<TProperty>? NaturalKey { get; }

    /// <inheritdoc/>
    public IReadOnlyList<IIndexDefinition<TProperty>> Indexes { get; }

    /// <inheritdoc/>
    public IDataLayout Layout { get; }

    /// <inheritdoc/>
    public IReadOnlyList<ISchemaDefinition<TProperty>>? Children { get; }

    /// <inheritdoc/>
    public string? PathExpression { get; }

    /// <inheritdoc/>
    public virtual TProperty? Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return default;
        }

        return Properties.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<TProperty> Get(IPropertyRole role)
    {
        if (role == null)
        {
            throw new ArgumentNullException(nameof(role));
        }

        return Properties
            .Where(p => string.Equals(p.Role.Name, role.Name, StringComparison.Ordinal))
            .ToList();
    }
}
