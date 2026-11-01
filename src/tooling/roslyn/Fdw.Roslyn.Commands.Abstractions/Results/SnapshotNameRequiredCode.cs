using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Snapshot name is required.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "SnapshotNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SnapshotNameRequiredCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotNameRequiredCode"/> class.
    /// </summary>
    public SnapshotNameRequiredCode()
        : base(21013, "SnapshotNameRequired",
            ResultSeverities.ByName("Error"),
            "Snapshot name is required",
            isRetryable: false)
    {
    }
}
