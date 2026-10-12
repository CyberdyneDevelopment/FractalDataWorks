using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace write failed part-way, and undoing the part that had already succeeded failed too.
/// </summary>
/// <remarks>
/// Creating a workspace also creates its per-workspace permissions, and deleting one removes them;
/// the two live in different domains, so neither pair commits as one. Each step that succeeds is
/// undone when the next fails. This code is the one case that cannot be undone automatically, so it
/// names the step that failed and what its undo left behind: somebody has to repair it by hand
/// (FDW-937). HTTP 500.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceCompensationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceCompensationFailedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceCompensationFailedCode"/> class.</summary>
    public WorkspaceCompensationFailedCode()
        : base(90003, "WorkspaceCompensationFailed",
            ResultSeverities.ByName("Critical"),
            "Workspace '{name}': {step} failed, and undoing it left {undo} behind; repair it by hand",
            isRetryable: false)
    {
    }
}
