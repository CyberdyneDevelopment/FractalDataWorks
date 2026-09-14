using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>What happens when someone asks to join a dataverse.</summary>
public interface IDataverseJoinPolicy : ITypeOption<int, DataverseJoinPolicyBase>
{
    /// <summary>Gets whether this policy accepts a membership request at all.</summary>
    bool AcceptsRequests { get; }

    /// <summary>Gets whether an accepted request grants membership immediately, with no review.</summary>
    bool AutoApproves { get; }
}
