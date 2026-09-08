using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Base for the states a membership can be in.</summary>
/// <remarks>
/// No id is passed — it derives from the option's fully qualified type name. The database stores
/// the NAME, so the name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class DataverseMemberStateBase : TypeOptionBase<DataverseMemberStateBase>, IDataverseMemberState
{
    /// <summary>Initializes a new instance of the <see cref="DataverseMemberStateBase"/> class.</summary>
    /// <param name="name">The state name, which is the value persisted.</param>
    /// <param name="grantsMembership">Whether a member in this state is actually a member.</param>
    protected DataverseMemberStateBase(string name, bool grantsMembership) : base(name)
    {
        GrantsMembership = grantsMembership;
    }

    /// <summary>Gets whether a member in this state holds the access their role describes.</summary>
    /// <remarks>
    /// The state answers whether they are here; the role answers what they may do. Both must say
    /// yes. This is on the option rather than decided by a caller comparing names, so a state added
    /// later arrives with its own answer instead of falling through somebody's else-branch.
    /// </remarks>
    public bool GrantsMembership { get; }
}
