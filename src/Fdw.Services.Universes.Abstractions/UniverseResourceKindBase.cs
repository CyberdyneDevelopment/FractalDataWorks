using Fdw.Collections;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>
/// Base class for the kinds of resource that can be attached to a universe.
/// </summary>
/// <remarks>
/// No id is passed. <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully
/// qualified type name, which is what makes options declarable from the packages that own their
/// resources — there is no central place handing out non-colliding numbers, and none is needed.
///
/// Nothing persists the derived id. <c>universe.UniverseResource.ResourceType</c> stores the
/// kind's name, so the name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class UniverseResourceKindBase
    : TypeOptionBase<UniverseResourceKindBase>, IUniverseResourceKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UniverseResourceKindBase"/> class.
    /// </summary>
    /// <param name="name">The kind name. This is the value persisted in the database.</param>
    /// <param name="canBeOwned">Whether a universe may own this kind, not merely use it.</param>
    protected UniverseResourceKindBase(string name, bool canBeOwned)
        : base(name)
    {
        CanBeOwned = canBeOwned;
    }

    /// <inheritdoc />
    public bool CanBeOwned { get; }
}
