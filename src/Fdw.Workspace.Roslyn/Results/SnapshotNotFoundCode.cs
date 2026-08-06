using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Snapshot with the specified ID was not found.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SnapshotNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SnapshotNotFoundCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotNotFoundCode"/> class.
    /// </summary>
    public SnapshotNotFoundCode()
        : base(31004, "SnapshotNotFound",
            ResultSeverities.ByName("Error"),
            "Snapshot not found: {SnapshotId}",
            isRetryable: false)
    {
    }
}