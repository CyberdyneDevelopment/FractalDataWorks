using Fdw.Collections;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Base for the kinds of thing that can hold a universe membership.</summary>
/// <remarks>
/// No id is passed — it derives from the fully qualified type name. The database stores the NAME.
/// </remarks>
public abstract class UniverseSubjectTypeBase : TypeOptionBase<UniverseSubjectTypeBase>, IUniverseSubjectType
{
    /// <summary>Initializes a new instance of the <see cref="UniverseSubjectTypeBase"/> class.</summary>
    /// <param name="name">The subject-type name, which is the value persisted.</param>
    protected UniverseSubjectTypeBase(string name) : base(name)
    {
    }
}
