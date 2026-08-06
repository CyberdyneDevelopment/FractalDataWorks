using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Solution path is required but was not provided.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SolutionPathRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SolutionPathRequiredCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionPathRequiredCode"/> class.
    /// </summary>
    public SolutionPathRequiredCode()
        : base(21001, "SolutionPathRequired",
            ResultSeverities.ByName("Error"),
            "Solution path is required.",
            isRetryable: false)
    {
    }
}