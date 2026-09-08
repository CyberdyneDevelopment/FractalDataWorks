using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Where a membership stands: whether the person is actually here.</summary>
public interface IDataverseMemberState : ITypeOption<int, DataverseMemberStateBase>
{
    /// <summary>Gets whether a member in this state holds the access their role describes.</summary>
    bool GrantsMembership { get; }
}
