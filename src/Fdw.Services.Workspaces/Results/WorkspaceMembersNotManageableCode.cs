using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The caller may view this workspace but may not decide who is in it.
/// </summary>
/// <remarks>
/// Category 10 (Forbidden), HTTP 403. Deciding membership is its own question (FDW-1071): the owner, a
/// member whose role manages members (Owner, Steward), or a holder of <c>workspaces:members</c>.
/// <c>workspaces:write</c> does not answer it.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceMembersNotManageable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceMembersNotManageableCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceMembersNotManageableCode"/> class.</summary>
    public WorkspaceMembersNotManageableCode()
        : base(100005, "WorkspaceMembersNotManageable",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' members cannot be managed by this caller: {reason}",
            isRetryable: false)
    {
    }
}
