using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// The workspace readiness report found no <c>WorkspaceReadinessClasses</c> option for a data set's class (FDW-4404).
/// </summary>
/// <remarks>
/// A fault in the platform's own code, not in the request: the classes are Bound, Partial, Sketched and Proposed and the
/// report picks one of them itself, so a miss means an option is missing. It is told as a server error rather than the
/// data set being counted as Proposed, as the code before this one did. HTTP 500.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceReadinessClassUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceReadinessClassUnknownCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceReadinessClassUnknownCode"/> class.</summary>
    public WorkspaceReadinessClassUnknownCode()
        : base(90012, "WorkspaceReadinessClassUnknown",
            ResultSeverities.ByName("Error"),
            "The readiness report has no class to count data set '{dataSet}' in",
            isRetryable: false)
    {
    }
}
