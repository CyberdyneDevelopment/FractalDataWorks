using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Anyone who can see it becomes a member on asking, with no review.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceJoinPolicies), "AutoApprove")]
public sealed class AutoApproveWorkspaceJoinPolicyOption : WorkspaceJoinPolicyBase
{
    /// <summary>Initializes a new instance of the <see cref="AutoApproveWorkspaceJoinPolicyOption"/> class.</summary>
    public AutoApproveWorkspaceJoinPolicyOption() : base("AutoApprove", acceptsRequests: true, autoApproves: true)
    {
    }
}
