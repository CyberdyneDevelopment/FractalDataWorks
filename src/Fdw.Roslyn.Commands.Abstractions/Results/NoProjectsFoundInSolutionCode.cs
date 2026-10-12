using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// No projects found in solution.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "NoProjectsFoundInSolution", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoProjectsFoundInSolutionCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoProjectsFoundInSolutionCode"/> class.
    /// </summary>
    public NoProjectsFoundInSolutionCode()
        : base(31006, "NoProjectsFoundInSolution",
            ResultSeverities.ByName("Error"),
            "No projects found in solution",
            isRetryable: false)
    {
    }
}
