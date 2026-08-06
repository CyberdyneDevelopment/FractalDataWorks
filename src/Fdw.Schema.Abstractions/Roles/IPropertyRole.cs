using Fdw.Collections;

namespace Fdw.Schema;

/// <summary>
/// Interface for property roles.
/// </summary>
/// <remarks>
/// <para>
/// PropertyRoles classify the semantic purpose of properties in a schema,
/// replacing magic strings and enabling compile-time validation.
/// </para>
/// <para>
/// Extends ITypeOption to enable MutableTypeCollection pattern with source generator discovery.
/// </para>
/// </remarks>
public interface IPropertyRole : ITypeOption<int, PropertyRoleBase>
{
    /// <summary>
    /// Gets the description of this role.
    /// </summary>
    /// <value>A human-readable description of the role's semantic meaning.</value>
    string Description { get; }

    /// <summary>
    /// Gets a value indicating whether this role is a key role (Surrogate or NaturalKey).
    /// </summary>
    /// <value>True if this property serves as a primary or alternate key; otherwise, false.</value>
    bool IsKeyRole { get; }

    /// <summary>
    /// Gets a value indicating whether properties with this role should be indexed.
    /// </summary>
    /// <value>True if this role typically requires indexing for performance; otherwise, false.</value>
    bool IsIndexable { get; }

    /// <summary>
    /// Gets a value indicating whether this role can be used in aggregate functions (SUM, AVG, etc.).
    /// </summary>
    /// <value>True if this property contains numeric data suitable for aggregation; otherwise, false.</value>
    bool IsAggregatable { get; }
}
