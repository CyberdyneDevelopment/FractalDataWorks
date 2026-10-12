using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results.Codes;

/// <summary>
/// The MSBuildWorkspace failed to load the solution.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynWorkspaceResultCodes), "WorkspaceLoadFailed", RestrictToCurrentCompilation = true)]
public sealed class WorkspaceLoadFailedCode : RoslynWorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceLoadFailedCode"/> class.
    /// </summary>
    public WorkspaceLoadFailedCode()
        : base(
            70000,
            "WorkspaceLoadFailed",
            ResultSeverities.ByName("Error"),
            "Failed to load workspace from {path}: {message}")
    {
    }
}
