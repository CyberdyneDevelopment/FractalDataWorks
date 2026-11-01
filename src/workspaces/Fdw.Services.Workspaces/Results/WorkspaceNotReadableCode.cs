using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A workspace-scoped query names a workspace that does not exist or that the caller may not read.
/// </summary>
/// <remarks>
/// One answer for both on purpose. The route is handed to third parties whose only grant is their
/// own workspaces; answering 404 for one case and 403 for the other would let a key enumerate every
/// workspace name in the tenant. The real reason is already logged — by the access policy as
/// <see cref="WorkspaceReadNotPermittedCode"/> when it refuses. HTTP 404.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceNotReadable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceNotReadableCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceNotReadableCode"/> class.</summary>
    public WorkspaceNotReadableCode()
        : base(30002, "WorkspaceNotReadable",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' was not found",
            isRetryable: false)
    {
    }
}
