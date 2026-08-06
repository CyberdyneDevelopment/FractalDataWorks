using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to delete the session.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SessionDeleteFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SessionDeleteFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionDeleteFailedCode"/> class.
    /// </summary>
    public SessionDeleteFailedCode()
        : base(71002, "SessionDeleteFailed",
            ResultSeverities.ByName("Error"),
            "Failed to delete session: {ErrorMessage}",
            isRetryable: true)
    {
    }
}