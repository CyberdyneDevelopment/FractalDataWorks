#pragma warning disable CS1591
using System.Collections.Generic;
using Fdw.Schema.Properties;

namespace Fdw.Schema.Keys;

/// <summary>
/// Defines a key (primary or unique) on a schema.
/// </summary>
/// <typeparam name="TProperty">The property definition type.</typeparam>
public interface IKeyDefinition<TProperty> where TProperty : IPropertyDefinition
{
    /// <summary>
    /// Gets the optional constraint name.
    /// </summary>
    /// <remarks>
    /// For SQL: PK_TableName or UQ_TableName_ColumnName.
    /// May be null for unnamed keys.
    /// </remarks>
    string? Name { get; }

    /// <summary>
    /// Gets the key members (columns) in ordinal order.
    /// </summary>
    IReadOnlyList<KeyMember> Members { get; }

    /// <summary>
    /// Gets a value indicating whether this is a composite key (multiple columns).
    /// </summary>
    bool IsComposite { get; }
}
