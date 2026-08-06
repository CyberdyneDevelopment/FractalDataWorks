using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Session with the specified ID was not found.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SessionNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SessionNotFoundCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionNotFoundCode"/> class.
    /// </summary>
    public SessionNotFoundCode()
        : base(31003, "SessionNotFound",
            ResultSeverities.ByName("Error"),
            "Session {SessionId} not found.",
            isRetryable: false)
    {
    }
}