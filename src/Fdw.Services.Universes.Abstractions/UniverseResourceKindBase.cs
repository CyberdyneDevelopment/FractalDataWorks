using Fdw.Collections;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>
/// Base class for the kinds of resource that can be attached to a universe.
/// </summary>
/// <remarks>
/// Keyed by an explicit integer, matching every other <c>[TypeOption]</c> collection in the
/// framework. A string key reads better here — the database stores the name, not the number — but
/// <c>TypeOptionExtensionGenerator</c> emits an <c>ById(int)</c> call for every option regardless
/// of the collection's key type, so a string-keyed collection declared this way does not compile.
/// Following the established shape is the right trade until that generator is fixed.
///
/// Ids are allocated from the table in <see cref="UniverseResourceKinds"/>. Options live in the
/// packages owning their resources, so that table is the one place a new domain looks to pick a
/// free number; a collision fails loud when the collection freezes rather than silently winning.
/// </remarks>
public abstract class UniverseResourceKindBase
    : TypeOptionBase<int, UniverseResourceKindBase>, IUniverseResourceKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UniverseResourceKindBase"/> class.
    /// </summary>
    /// <param name="id">The kind's unique id, allocated from the table in <see cref="UniverseResourceKinds"/>.</param>
    /// <param name="name">The kind name. This is the value persisted in the database.</param>
    /// <param name="displayName">The name shown in the UI.</param>
    /// <param name="description">What this kind of resource is.</param>
    /// <param name="canBeOwned">Whether a universe may own this kind, not merely use it.</param>
    protected UniverseResourceKindBase(
        int id,
        string name,
        string displayName,
        string description,
        bool canBeOwned)
        : base(id, name, $"UniverseResourceKinds:{name}", displayName, description, "UniverseResourceKind")
    {
        CanBeOwned = canBeOwned;
    }

    /// <inheritdoc />
    public bool CanBeOwned { get; }
}
