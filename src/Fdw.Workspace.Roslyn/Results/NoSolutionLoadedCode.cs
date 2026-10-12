using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// No solution is loaded. User must load a solution first.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "NoSolutionLoaded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoSolutionLoadedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoSolutionLoadedCode"/> class.
    /// </summary>
    public NoSolutionLoadedCode()
        : base(41000, "NoSolutionLoaded",
            ResultSeverities.ByName("Error"),
            "No solution is loaded. Use the OpenSolution tool to load a solution first.",
            isRetryable: false)
    {
    }
}