using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to create a new session.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SessionCreationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SessionCreationFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionCreationFailedCode"/> class.
    /// </summary>
    public SessionCreationFailedCode()
        : base(90001, "SessionCreationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to create session for '{SolutionPath}': {ErrorMessage}",
            isRetryable: true)
    {
    }
}