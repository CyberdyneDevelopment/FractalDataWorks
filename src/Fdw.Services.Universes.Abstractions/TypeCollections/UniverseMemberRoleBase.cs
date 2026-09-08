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
    /// <param name="mayWrite">Whether this role may change the universe.</param>
    protected UniverseMemberRoleBase(string name, bool mayWrite) : base(name)
    {
        MayWrite = mayWrite;
    }

    /// <summary>Gets whether this role may change the universe it is held in.</summary>
    /// <remarks>
    /// On the option, not decided by a caller comparing names, so a role added later arrives with
    /// its own answer instead of falling through somebody's else-branch. This is the only question
    /// the vocabulary answers so far: Owner's ability to change WHO ELSE is in the project is a
    /// separate decision, and declaring a property nothing reads is the failure this fixes.
    /// </remarks>
    public bool MayWrite { get; }
}
