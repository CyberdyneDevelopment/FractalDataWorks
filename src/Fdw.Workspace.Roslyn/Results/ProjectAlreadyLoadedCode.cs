using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Project is already loaded in the workspace.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ProjectAlreadyLoaded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectAlreadyLoadedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectAlreadyLoadedCode"/> class.
    /// </summary>
    public ProjectAlreadyLoadedCode()
        : base(40001, "ProjectAlreadyLoaded",
            ResultSeverities.ByName("Warning"),
            "Project '{ProjectName}' is already loaded",
            isRetryable: false)
    {
    }
}