using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Anyone who can see it may ask; an owner or steward reviews.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceJoinPolicies), "RequestToJoin")]
public sealed class RequestToJoinWorkspaceJoinPolicyOption : WorkspaceJoinPolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="RequestToJoinWorkspaceJoinPolicyOption"/> class.</summary>
    public RequestToJoinWorkspaceJoinPolicyOption() : base("RequestToJoin", acceptsRequests: true, autoApproves: false)
    {
    }
}
