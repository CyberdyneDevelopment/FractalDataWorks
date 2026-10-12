using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to load the workspace.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "WorkspaceLoadFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceLoadFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceLoadFailedCode"/> class.
    /// </summary>
    public WorkspaceLoadFailedCode()
        : base(71006, "WorkspaceLoadFailed",
            ResultSeverities.ByName("Error"),
            "Failed to load workspace: {ErrorMessage}",
            isRetryable: true)
    {
    }
}