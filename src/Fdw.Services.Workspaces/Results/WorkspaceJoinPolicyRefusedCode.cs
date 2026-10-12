using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The workspace's own join policy does not accept a membership request at all (Closed).
/// </summary>
/// <remarks>
/// Category 10 (Forbidden), not Category 5 (Auth) like <see cref="WorkspaceWriteNotPermittedCode"/>:
/// the caller is exactly who they say they are and needs no further authentication — the refusal is
/// about the workspace's own configuration, not about who is asking. HTTP 403, not 401.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceJoinPolicyRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceJoinPolicyRefusedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceJoinPolicyRefusedCode"/> class.</summary>
    public WorkspaceJoinPolicyRefusedCode()
        : base(100000, "WorkspaceJoinPolicyRefused",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' does not accept membership requests: {reason}",
            isRetryable: false)
    {
    }
}
