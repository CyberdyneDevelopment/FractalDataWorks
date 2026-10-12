using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace was deleted, but its own permissions could not be removed.
/// </summary>
/// <remarks>
/// The delete itself happened. The permissions left behind are named by the deleted workspace's Id, so
/// they can never grant anything, but they stay in the permission catalogue with their role grants
/// until removed by hand; the failure says so rather than reporting a clean delete (FDW-937). HTTP 500.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspacePermissionsNotRemoved", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspacePermissionsNotRemovedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspacePermissionsNotRemovedCode"/> class.</summary>
    public WorkspacePermissionsNotRemovedCode()
        : base(90004, "WorkspacePermissionsNotRemoved",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' was deleted, but its permissions workspaces/{id}:view and :query could not be removed ({reason}); re-running the seed retires them",
            isRetryable: false)
    {
    }
}
