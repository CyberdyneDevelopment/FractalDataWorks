using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Project is not loaded in the workspace.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ProjectNotLoaded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectNotLoadedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectNotLoadedCode"/> class.
    /// </summary>
    public ProjectNotLoadedCode()
        : base(41003, "ProjectNotLoaded",
            ResultSeverities.ByName("Error"),
            "Project '{ProjectName}' is not loaded",
            isRetryable: false)
    {
    }
}