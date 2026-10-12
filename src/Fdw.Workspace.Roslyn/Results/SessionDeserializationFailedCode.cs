using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Failed to deserialize the session data.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SessionDeserializationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SessionDeserializationFailedCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionDeserializationFailedCode"/> class.
    /// </summary>
    public SessionDeserializationFailedCode()
        : base(90003, "SessionDeserializationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to deserialize session.",
            isRetryable: false)
    {
    }
}