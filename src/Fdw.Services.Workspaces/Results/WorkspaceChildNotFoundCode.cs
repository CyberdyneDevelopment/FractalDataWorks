using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A member, resource or relationship named for a narrow write is not in that workspace.
/// </summary>
/// <remarks>
/// Distinct from the workspace itself being absent: the project exists and the row does not, which
/// usually means someone else removed it between the caller reading the map and writing to it.
/// HTTP 404.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceChildNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceChildNotFoundCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceChildNotFoundCode"/> class.</summary>
    public WorkspaceChildNotFoundCode()
        : base(30000, "WorkspaceChildNotFound",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' has no {kind} with id '{id}'",
            isRetryable: false)
    {
    }
}
