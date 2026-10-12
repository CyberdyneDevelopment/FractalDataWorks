using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Requests are not accepted. Membership is by invitation only.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceJoinPolicies), "Closed")]
public sealed class ClosedWorkspaceJoinPolicyOption : WorkspaceJoinPolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="ClosedWorkspaceJoinPolicyOption"/> class.</summary>
    public ClosedWorkspaceJoinPolicyOption() : base("Closed", acceptsRequests: false, autoApproves: false)
    {
    }
}
