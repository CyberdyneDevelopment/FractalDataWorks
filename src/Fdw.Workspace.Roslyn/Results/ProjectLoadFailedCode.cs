using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to load the project.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ProjectLoadFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectLoadFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectLoadFailedCode"/> class.
    /// </summary>
    public ProjectLoadFailedCode()
        : base(71001, "ProjectLoadFailed",
            ResultSeverities.ByName("Error"),
            "Failed to load project '{ProjectName}': {ErrorMessage}",
            isRetryable: true)
    {
    }
}