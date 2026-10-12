using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The caller may not query the data sets of this workspace: they neither own it, hold a membership
/// whose role reads, nor hold <c>workspaces:query</c> or <c>workspaces/{id}:query</c>.
/// </summary>
/// <remarks>
/// Category 10 (Forbidden), like <see cref="WorkspaceJoinPolicyRefusedCode"/> and unlike the Auth
/// category 5, which maps to 401 and would send a correctly signed-in caller back to sign in (FDW-876,
/// FDW-937). HTTP 403. The workspace-scoped query route does not answer with this code: it
/// answers <see cref="WorkspaceNotReadableCode"/> so a refusal and an absence look the same.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceReadNotPermitted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceReadNotPermittedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceReadNotPermittedCode"/> class.</summary>
    public WorkspaceReadNotPermittedCode()
        : base(100001, "WorkspaceReadNotPermitted",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' cannot be read by this caller: {reason}",
            isRetryable: false)
    {
    }
}
