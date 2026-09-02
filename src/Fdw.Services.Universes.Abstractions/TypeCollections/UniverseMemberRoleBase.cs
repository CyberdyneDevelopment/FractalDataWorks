using Fdw.Collections;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>Base for the roles a person can hold in a universe.</summary>
/// <remarks>
/// No id is passed — it derives from the option's fully qualified type name. The database stores
/// the NAME, so the name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class UniverseMemberRoleBase : TypeOptionBase<UniverseMemberRoleBase>, IUniverseMemberRole
{
    /// <summary>Initializes a new instance of the <see cref="UniverseMemberRoleBase"/> class.</summary>
    /// <param name="name">The role name, which is the value persisted.</param>
    protected UniverseMemberRoleBase(string name) : base(name)
    {
    }
}
