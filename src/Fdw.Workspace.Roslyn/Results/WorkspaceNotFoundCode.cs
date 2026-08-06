using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Workspace with the specified ID was not found.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "WorkspaceNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceNotFoundCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceNotFoundCode"/> class.
    /// </summary>
    public WorkspaceNotFoundCode()
        : base(31006, "WorkspaceNotFound",
            ResultSeverities.ByName("Error"),
            "Workspace {WorkspaceId} not found.",
            isRetryable: false)
    {
    }
}