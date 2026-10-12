using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Project was not found in the current (filtered) solution.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ProjectNotFoundInCurrentSolution", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectNotFoundInCurrentSolutionCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectNotFoundInCurrentSolutionCode"/> class.
    /// </summary>
    public ProjectNotFoundInCurrentSolutionCode()
        : base(31000, "ProjectNotFoundInCurrentSolution",
            ResultSeverities.ByName("Error"),
            "Project '{ProjectName}' not found in current solution",
            isRetryable: false)
    {
    }
}