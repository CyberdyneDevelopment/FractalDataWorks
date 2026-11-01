using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A data set named in a workspace-scoped query is not attached to that workspace.
/// </summary>
/// <remarks>
/// Deliberately the same answer whether the data set exists elsewhere or not at all: a caller whose
/// only grant is this workspace must not learn which data sets exist outside it. HTTP 404.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceDataSetNotAttached", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceDataSetNotAttachedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceDataSetNotAttachedCode"/> class.</summary>
    public WorkspaceDataSetNotAttachedCode()
        : base(30001, "WorkspaceDataSetNotAttached",
            ResultSeverities.ByName("Error"),
            "Workspace '{name}' has no data set '{dataSet}'",
            isRetryable: false)
    {
    }
}
