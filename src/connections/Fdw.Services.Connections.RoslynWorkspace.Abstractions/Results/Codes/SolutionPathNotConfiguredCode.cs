using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results.Codes;

/// <summary>
/// The RoslynWorkspace connection configuration is missing the required SolutionPath.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynWorkspaceResultCodes), "SolutionPathNotConfigured", RestrictToCurrentCompilation = true)]
public sealed class SolutionPathNotConfiguredCode : RoslynWorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionPathNotConfiguredCode"/> class.
    /// </summary>
    public SolutionPathNotConfiguredCode()
        : base(
            60000,
            "SolutionPathNotConfigured",
            ResultSeverities.ByName("Error"),
            "RoslynWorkspaceConnection {connection} missing required SolutionPath")
    {
    }
}
