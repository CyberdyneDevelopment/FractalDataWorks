using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// Whether the caller may use a workspace could not be decided, because something the decision needs
/// could not be read.
/// </summary>
/// <remarks>
/// Not a refusal. Answering an unreadable role store with "no access" would turn an outage into an
/// empty list or a 404 that nobody investigates (FDW-937), so the access policy fails with this code
/// instead and every caller of the policy passes it on. HTTP 500.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceAccessUndetermined", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceAccessUndeterminedCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceAccessUndeterminedCode"/> class.</summary>
    public WorkspaceAccessUndeterminedCode()
        : base(90005, "WorkspaceAccessUndetermined",
            ResultSeverities.ByName("Error"),
            "Access to workspace '{name}' could not be decided: {reason}",
            isRetryable: true)
    {
    }
}
