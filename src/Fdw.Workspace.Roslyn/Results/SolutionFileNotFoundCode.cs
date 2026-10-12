using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Solution file was not found at the specified path.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SolutionFileNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SolutionFileNotFoundCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionFileNotFoundCode"/> class.
    /// </summary>
    public SolutionFileNotFoundCode()
        : base(31005, "SolutionFileNotFound",
            ResultSeverities.ByName("Error"),
            "Solution file not found: {SolutionPath}",
            isRetryable: false)
    {
    }
}