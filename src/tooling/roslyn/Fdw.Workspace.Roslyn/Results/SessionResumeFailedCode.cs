using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to resume an existing session.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SessionResumeFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SessionResumeFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionResumeFailedCode"/> class.
    /// </summary>
    public SessionResumeFailedCode()
        : base(91000, "SessionResumeFailed",
            ResultSeverities.ByName("Error"),
            "Failed to resume session {SessionId}: {ErrorMessage}",
            isRetryable: true)
    {
    }
}