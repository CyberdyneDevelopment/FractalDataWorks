using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Project was not found in the solution.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ProjectNotFoundInSolution", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectNotFoundInSolutionCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectNotFoundInSolutionCode"/> class.
    /// </summary>
    public ProjectNotFoundInSolutionCode()
        : base(31002, "ProjectNotFoundInSolution",
            ResultSeverities.ByName("Error"),
            "Project '{ProjectName}' not found in solution",
            isRetryable: false)
    {
    }
}