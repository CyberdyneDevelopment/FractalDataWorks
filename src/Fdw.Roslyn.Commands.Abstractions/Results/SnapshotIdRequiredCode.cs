using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Snapshot ID is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "SnapshotIdRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SnapshotIdRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotIdRequiredCode"/> class.
    /// </summary>
    public SnapshotIdRequiredCode()
        : base(21012, "SnapshotIdRequired",
            ResultSeverities.ByName("Error"),
            "Snapshot ID is required",
            isRetryable: false)
    {
    }
}
