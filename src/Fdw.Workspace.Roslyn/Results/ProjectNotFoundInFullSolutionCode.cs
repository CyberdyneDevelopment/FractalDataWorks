using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Project was not found in the full solution (before filtering).
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ProjectNotFoundInFullSolution", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectNotFoundInFullSolutionCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectNotFoundInFullSolutionCode"/> class.
    /// </summary>
    public ProjectNotFoundInFullSolutionCode()
        : base(31001, "ProjectNotFoundInFullSolution",
            ResultSeverities.ByName("Error"),
            "Project '{ProjectName}' not found in full solution",
            isRetryable: false)
    {
    }
}