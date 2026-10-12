using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Project cannot be unloaded because other projects depend on it.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ProjectHasDependents", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectHasDependentsCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectHasDependentsCode"/> class.
    /// </summary>
    public ProjectHasDependentsCode()
        : base(41001, "ProjectHasDependents",
            ResultSeverities.ByName("Error"),
            "Cannot unload project '{ProjectName}': it is referenced by: {Dependents}",
            isRetryable: false)
    {
    }
}