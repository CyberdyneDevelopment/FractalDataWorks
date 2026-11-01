using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace was deleted, but a variation it owned could not be retired.
/// </summary>
/// <remarks>
/// The delete itself happened. A variation exists only as its workspace's own view of a data set, and nothing else
/// can delete one, so the one left behind stays owned by a deleted workspace and keeps its name until it is removed
/// by hand; the failure says so rather than reporting a clean delete (FDW-1027). HTTP 500.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceVariationsNotRemoved", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceVariationsNotRemovedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceVariationsNotRemovedCode"/> class.</summary>
    public WorkspaceVariationsNotRemovedCode()
        : base(90006, "WorkspaceVariationsNotRemoved",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' was deleted, but its variation '{variation}' could not be retired ({reason}); it stays owned by the deleted workspace until removed by hand",
            isRetryable: false)
    {
    }
}
