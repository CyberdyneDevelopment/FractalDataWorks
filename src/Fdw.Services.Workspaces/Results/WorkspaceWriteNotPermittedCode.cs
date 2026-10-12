using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The caller may view this workspace but is neither its owner, nor a member of it with a role that
/// may change it, nor a holder of <c>workspaces:write</c>.
/// </summary>
/// <remarks>
/// Category 10 (Forbidden), like <see cref="WorkspaceReadNotPermittedCode"/>. It was 51000 (Auth,
/// category 5), which maps to 401 and sent a correctly signed-in caller back to sign in for a refusal
/// about this resource (FDW-936). HTTP 403.
/// <para>
/// Any one of three grants the change: owning the workspace, a membership whose role may change it,
/// or <c>workspaces:write</c> (FDW-936: a platform permission OR standing in the workspace, never
/// both required).
/// </para>
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceWriteNotPermitted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceWriteNotPermittedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceWriteNotPermittedCode"/> class.</summary>
    public WorkspaceWriteNotPermittedCode()
        : base(100002, "WorkspaceWriteNotPermitted",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' cannot be changed by this caller: {reason}",
            isRetryable: false)
    {
    }
}
