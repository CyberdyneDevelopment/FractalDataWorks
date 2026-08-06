using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Results.ExecutionStatus;

/// <summary>
/// Running execution status - execution in progress.
/// </summary>
[TypeOption(typeof(ExecutionStatuses), "Running", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RunningStatus : ExecutionStatusBase
{
    /// <summary>
    /// MudBlazor icon for running status.
    /// </summary>
    public const string RunningIcon = "play_circle"; // Icons.Material.Filled.PlayCircle

    /// <summary>
    /// Initializes a new instance of the <see cref="RunningStatus"/> class.
    /// </summary>
    public RunningStatus()
        : base(
            id: 2,
            name: "Running",
            icon: RunningIcon,
            color: "Info",
            isTerminal: false,
            isSuccess: false,
            isInProgress: true)
    {
    }
}
