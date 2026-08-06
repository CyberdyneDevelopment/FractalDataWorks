using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// CRTP base class for key type options.
/// </summary>
/// <remarks>
/// Tactical flags carried since the original implementation: <see cref="IsPrimaryKey"/>,
/// <see cref="HasConstraint"/>, <see cref="IsReference"/>, <see cref="IsSystemGenerated"/>.
/// <para>
/// <see cref="SupportsUniqueness"/> is a capability property translators use to ask the key type what
/// it can do (see <see cref="ContainerKeyExtensions.CanEnforceUniqueness"/>) rather than branching on
/// type strings. It is <c>virtual</c> with a safe <see langword="false"/> default; the known TypeOptions
/// override it per the capability matrix.
/// </para>
/// </remarks>
public abstract class KeyTypeBase : TypeOptionBase<int, KeyTypeBase>, IKeyType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyTypeBase"/> class.
    /// </summary>
    protected KeyTypeBase(
        int id,
        string name,
        bool isPrimaryKey,
        bool hasConstraint,
        bool isReference,
        bool isSystemGenerated)
        : base(id, name)
    {
        IsPrimaryKey = isPrimaryKey;
        HasConstraint = hasConstraint;
        IsReference = isReference;
        IsSystemGenerated = isSystemGenerated;
    }

    /// <inheritdoc />
    public bool IsPrimaryKey { get; }

    /// <inheritdoc />
    public bool HasConstraint { get; }

    /// <inheritdoc />
    public bool IsReference { get; }

    /// <inheritdoc />
    public bool IsSystemGenerated { get; }

    /// <summary>
    /// Gets a value indicating whether this key type intrinsically supports uniqueness
    /// (independent of whether a specific key is physically enforced — see
    /// <see cref="ContainerKeyExtensions.CanEnforceUniqueness"/>).
    /// </summary>
    public virtual bool SupportsUniqueness => false;
}
