using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results.Codes;

/// <summary>
/// The .sln file specified in SolutionPath does not exist on disk.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynWorkspaceResultCodes), "SolutionFileNotFound", RestrictToCurrentCompilation = true)]
public sealed class SolutionFileNotFoundCode : RoslynWorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionFileNotFoundCode"/> class.
    /// </summary>
    public SolutionFileNotFoundCode()
        : base(
            30000,
            "SolutionFileNotFound",
            ResultSeverities.ByName("Error"),
            "Solution file not found: {path}")
    {
    }
}
