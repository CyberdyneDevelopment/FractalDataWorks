namespace Fdw.Data.Abstractions;

/// <summary>
/// A single field entry within an <see cref="IContainerKey"/>, establishing the relationship
/// between a local container field and (optionally) a referenced container field.
/// </summary>
/// <remarks>
/// <see cref="ReferencedField"/> is <see langword="null"/> for non-referencing key types
/// (Primary, Surrogate, Natural). It is populated for Foreign keys that reference a parent
/// container's field. Join keys may or may not have a referenced field depending on whether
/// the join target is resolved.
/// <para>
/// Why: <see cref="ReferencedField"/> is nullable rather than promoted to a typed
/// <c>IForeignKeyField</c> subtype. <see cref="IContainerKey.KeyType"/> is the discriminator;
/// a typed subtype would duplicate it and force heterogeneous <c>KeyFields</c> collections.
/// The loader enforces the invariant at construction; actors pattern-match defensively.
/// </para>
/// </remarks>
public interface IContainerKeyField
{
    /// <summary>
    /// Gets the field on the local container that participates in this key.
    /// </summary>
    IDataField LocalField { get; }

    /// <summary>
    /// Gets the field on the referenced container, if this is a foreign or join key relationship.
    /// </summary>
    /// <value>
    /// The referenced <see cref="IDataField"/>, or <see langword="null"/> when this key field
    /// does not reference another container.
    /// </value>
    IDataField? ReferencedField { get; }

    /// <summary>
    /// Gets the zero-based position of this field within the composite key.
    /// </summary>
    int Ordinal { get; }
}
