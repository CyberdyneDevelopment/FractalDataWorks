using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Persisted session file was not found in storage.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "PersistedSessionNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PersistedSessionNotFoundCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistedSessionNotFoundCode"/> class.
    /// </summary>
    public PersistedSessionNotFoundCode()
        : base(30000, "PersistedSessionNotFound",
            ResultSeverities.ByName("Warning"),
            "Persisted session {SessionId} not found in system store",
            isRetryable: false)
    {
    }
}