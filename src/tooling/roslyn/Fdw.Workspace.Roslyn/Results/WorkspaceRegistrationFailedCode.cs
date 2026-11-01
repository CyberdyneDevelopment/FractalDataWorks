using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to register the workspace in the manager.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "WorkspaceRegistrationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceRegistrationFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceRegistrationFailedCode"/> class.
    /// </summary>
    public WorkspaceRegistrationFailedCode()
        : base(91001, "WorkspaceRegistrationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to register workspace.",
            isRetryable: true)
    {
    }
}