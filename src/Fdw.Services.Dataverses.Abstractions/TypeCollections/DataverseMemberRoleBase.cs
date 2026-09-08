using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Base for the roles a person can hold in a dataverse.</summary>
/// <remarks>
/// No id is passed — it derives from the option's fully qualified type name. The database stores
/// the NAME, so the name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class DataverseMemberRoleBase : TypeOptionBase<DataverseMemberRoleBase>, IDataverseMemberRole
{
    /// <summary>Initializes a new instance of the <see cref="DataverseMemberRoleBase"/> class.</summary>
    /// <param name="name">The role name, which is the value persisted.</param>
    /// <param name="mayWrite">Whether this role may change the dataverse.</param>
    protected DataverseMemberRoleBase(string name, bool mayWrite) : base(name)
    {
        MayWrite = mayWrite;
    }

    /// <summary>Gets whether this role may change the dataverse it is held in.</summary>
    /// <remarks>
    /// On the option, not decided by a caller comparing names, so a role added later arrives with
    /// its own answer instead of falling through somebody's else-branch. This is the only question
    /// the vocabulary answers so far: Owner's ability to change WHO ELSE is in the project is a
    /// separate decision, and declaring a property nothing reads is the failure this fixes.
    /// </remarks>
    public bool MayWrite { get; }
}
