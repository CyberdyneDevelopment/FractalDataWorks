using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Project has pending changes and cannot be unloaded without force.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ProjectHasPendingChanges", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectHasPendingChangesCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectHasPendingChangesCode"/> class.
    /// </summary>
    public ProjectHasPendingChangesCode()
        : base(41002, "ProjectHasPendingChanges",
            ResultSeverities.ByName("Warning"),
            "Project '{ProjectName}' has pending changes. Use force=true to unload anyway.",
            isRetryable: false)
    {
    }
}