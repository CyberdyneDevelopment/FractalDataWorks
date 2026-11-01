using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The caller cannot see this workspace at all: it is not listed for them and they hold no standing
/// in it, so it answers exactly as a workspace that does not exist.
/// </summary>
/// <remarks>
/// HTTP 404, for the reason <see cref="WorkspaceNotReadableCode"/> gives on the query route: answering
/// 403 would confirm the name exists, and a caller who can see nothing could enumerate Private
/// workspaces one name at a time. The real reason is logged by the access policy (FDW-936).
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceNotVisible", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceNotVisibleCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceNotVisibleCode"/> class.</summary>
    public WorkspaceNotVisibleCode()
        : base(30003, "WorkspaceNotVisible",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' was not found",
            isRetryable: false)
    {
    }
}
