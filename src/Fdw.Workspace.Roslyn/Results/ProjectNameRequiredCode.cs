using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Project name is required but was not provided.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "ProjectNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProjectNameRequiredCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectNameRequiredCode"/> class.
    /// </summary>
    public ProjectNameRequiredCode()
        : base(20000, "ProjectNameRequired",
            ResultSeverities.ByName("Error"),
            "Project name is required",
            isRetryable: false)
    {
    }
}