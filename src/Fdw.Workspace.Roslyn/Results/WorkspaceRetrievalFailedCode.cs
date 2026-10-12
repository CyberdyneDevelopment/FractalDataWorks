using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Workspace was loaded but could not be retrieved.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "WorkspaceRetrievalFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceRetrievalFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceRetrievalFailedCode"/> class.
    /// </summary>
    public WorkspaceRetrievalFailedCode()
        : base(91002, "WorkspaceRetrievalFailed",
            ResultSeverities.ByName("Error"),
            "Workspace was loaded but could not be retrieved.",
            isRetryable: true)
    {
    }
}