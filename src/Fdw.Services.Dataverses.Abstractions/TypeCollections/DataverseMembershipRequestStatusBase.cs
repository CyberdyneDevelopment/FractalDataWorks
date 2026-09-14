using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Base for the states a membership request can be in.</summary>
/// <remarks>
/// No id is passed — it derives from the option's fully qualified type name. The database stores
/// the NAME, so the name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class DataverseMembershipRequestStatusBase : TypeOptionBase<DataverseMembershipRequestStatusBase>, IDataverseMembershipRequestStatus
{
    /// <summary>Initializes a new instance of the <see cref="DataverseMembershipRequestStatusBase"/> class.</summary>
    /// <param name="name">The status name, which is the value persisted.</param>
    protected DataverseMembershipRequestStatusBase(string name) : base(name)
    {
    }
}
