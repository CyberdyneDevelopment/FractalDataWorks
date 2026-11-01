using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace was to be created or deleted, but no <see cref="IWorkspacePermissions"/> is registered
/// to create or remove its own permissions.
/// </summary>
/// <remarks>
/// Checked before the workspace is touched, so a host missing the registration refuses cleanly rather
/// than leaving a workspace without its permissions (FDW-937). A setup error, not the caller's. HTTP 500.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspacePermissionsUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspacePermissionsUnavailableCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspacePermissionsUnavailableCode"/> class.</summary>
    public WorkspacePermissionsUnavailableCode()
        : base(60000, "WorkspacePermissionsUnavailable",
            ResultSeverities.ByName("Critical"),
            "Workspace '{name}' was not {step}: no IWorkspacePermissions is registered",
            isRetryable: false)
    {
    }
}
