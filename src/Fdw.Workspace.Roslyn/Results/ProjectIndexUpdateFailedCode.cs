using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to update project session index.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ProjectIndexUpdateFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectIndexUpdateFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectIndexUpdateFailedCode"/> class.
    /// </summary>
    public ProjectIndexUpdateFailedCode()
        : base(71000, "ProjectIndexUpdateFailed",
            ResultSeverities.ByName("Error"),
            "Failed to update project session index '{ProjectPath}': {ErrorMessage}",
            isRetryable: true)
    {
    }
}