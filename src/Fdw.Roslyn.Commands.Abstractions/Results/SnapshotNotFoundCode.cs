using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Snapshot not found.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "SnapshotNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SnapshotNotFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotNotFoundCode"/> class.
    /// </summary>
    public SnapshotNotFoundCode()
        : base(31017, "SnapshotNotFound",
            ResultSeverities.ByName("Error"),
            "Snapshot not found: {SnapshotId}",
            isRetryable: false)
    {
    }
}
