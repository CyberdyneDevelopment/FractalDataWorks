using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn.Results;

/// <summary>
/// Snapshot ID is required but was null or empty.
/// </summary>
[TypeOption(typeof(WorkspaceResultCodes), "SnapshotIdRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SnapshotIdRequiredCode : WorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotIdRequiredCode"/> class.
    /// </summary>
    public SnapshotIdRequiredCode()
        : base(21000, "SnapshotIdRequired",
            ResultSeverities.ByName("Error"),
            "Snapshot ID cannot be null or empty",
            isRetryable: false)
    {
    }
}