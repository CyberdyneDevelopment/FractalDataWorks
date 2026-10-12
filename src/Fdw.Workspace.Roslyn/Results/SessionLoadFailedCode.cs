using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to load the session.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SessionLoadFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SessionLoadFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionLoadFailedCode"/> class.
    /// </summary>
    public SessionLoadFailedCode()
        : base(71003, "SessionLoadFailed",
            ResultSeverities.ByName("Error"),
            "Failed to load session: {ErrorMessage}",
            isRetryable: true)
    {
    }
}