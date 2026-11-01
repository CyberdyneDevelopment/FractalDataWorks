using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The caller manages members of this workspace but the role involved ranks above their own.
/// </summary>
/// <remarks>
/// Category 10 (Forbidden), HTTP 403. A member grants, changes and removes only roles at or below their
/// own rank (FDW-1071), so a Steward cannot make an Owner or remove one.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceMemberRankExceeded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceMemberRankExceededCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceMemberRankExceededCode"/> class.</summary>
    public WorkspaceMemberRankExceededCode()
        : base(100006, "WorkspaceMemberRankExceeded",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' member change refused: {reason}",
            isRetryable: false)
    {
    }
}
