using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Workspaces.Results;

/// <summary>A note was rejected before it was written.</summary>
/// <remarks>
/// Rejection rather than repair: an over-long body is refused with its length and the limit named,
/// never truncated. A truncation is unrecoverable — the writer sees the note posted and does not
/// see that the end of it is gone.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "WorkspaceNoteInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WorkspaceNoteInvalidCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceNoteInvalidCode"/> class.</summary>
    public WorkspaceNoteInvalidCode()
        : base(20002, "WorkspaceNoteInvalid",
            ResultSeverities.ByName("Error"),
            "Note in workspace '{name}' rejected: {reason}",
            isRetryable: false)
    {
    }
}
