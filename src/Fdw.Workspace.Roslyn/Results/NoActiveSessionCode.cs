using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// No active session exists.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "NoActiveSession", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoActiveSessionCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoActiveSessionCode"/> class.
    /// </summary>
    public NoActiveSessionCode()
        : base(40000, "NoActiveSession",
            ResultSeverities.ByName("Warning"),
            "No active session. Use CreateSession or ResumeSession first.",
            isRetryable: false)
    {
    }
}