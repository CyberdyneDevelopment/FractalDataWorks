using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Results.ExecutionStatus;

/// <summary>
/// Skipped execution status - execution was skipped.
/// </summary>
[TypeOption(typeof(ExecutionStatuses), "Skipped", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SkippedStatus : ExecutionStatusBase
{
    /// <summary>
    /// Material Symbols icon name for skipped status.
    /// </summary>
    public const string SkippedIcon = "skip_next";

    /// <summary>
    /// Initializes a new instance of the <see cref="SkippedStatus"/> class.
    /// </summary>
    public SkippedStatus()
        : base(
            name: "Skipped",
            icon: SkippedIcon,
            color: "Default",
            isTerminal: true,
            isSuccess: true,
            isInProgress: false)
    {
    }
}
