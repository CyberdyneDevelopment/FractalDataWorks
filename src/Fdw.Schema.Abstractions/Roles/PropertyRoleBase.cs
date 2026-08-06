using Fdw.Collections;

namespace Fdw.Schema;

/// <summary>
/// Base class for property roles using CRTP pattern.
/// </summary>
/// <remarks>
/// <para>
/// Provides the foundation for all property role implementations.
/// Each role defines its semantic characteristics: whether it's a key,
/// indexable, or aggregatable.
/// </para>
/// <para>
/// Properties are set in constructor so TypeCollection source generator can read them
/// without instantiation.
/// </para>
/// </remarks>
public abstract class PropertyRoleBase : TypeOptionBase<int, PropertyRoleBase>, IPropertyRole
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyRoleBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this role.</param>
    /// <param name="name">Name of the role (must match TypeOption attribute).</param>
    /// <param name="description">Human-readable description of the role.</param>
    /// <param name="isKeyRole">Whether this role represents a key (primary or alternate).</param>
    /// <param name="isIndexable">Whether properties with this role should be indexed.</param>
    /// <param name="isAggregatable">Whether this role can be used in aggregate functions.</param>
    protected PropertyRoleBase(
        int id,
        string name,
        string description,
        bool isKeyRole,
        bool isIndexable,
        bool isAggregatable)
        : base(id, name, $"PropertyRoles:{name}", name, description, "PropertyRole")
    {
        IsKeyRole = isKeyRole;
        IsIndexable = isIndexable;
        IsAggregatable = isAggregatable;
    }

    /// <summary>
    /// Gets a value indicating whether this role is a key role (Surrogate or NaturalKey).
    /// </summary>
    /// <value>True if this property serves as a primary or alternate key; otherwise, false.</value>
    public bool IsKeyRole { get; }

    /// <summary>
    /// Gets a value indicating whether properties with this role should be indexed.
    /// </summary>
    /// <value>True if this role typically requires indexing for performance; otherwise, false.</value>
    public bool IsIndexable { get; }

    /// <summary>
    /// Gets a value indicating whether this role can be used in aggregate functions (SUM, AVG, etc.).
    /// </summary>
    /// <value>True if this property contains numeric data suitable for aggregation; otherwise, false.</value>
    public bool IsAggregatable { get; }
}
