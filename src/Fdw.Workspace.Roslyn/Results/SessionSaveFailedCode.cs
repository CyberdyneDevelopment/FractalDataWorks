using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to save the session.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SessionSaveFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SessionSaveFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionSaveFailedCode"/> class.
    /// </summary>
    public SessionSaveFailedCode()
        : base(71004, "SessionSaveFailed",
            ResultSeverities.ByName("Error"),
            "Failed to save session: {ErrorMessage}",
            isRetryable: true)
    {
    }
}