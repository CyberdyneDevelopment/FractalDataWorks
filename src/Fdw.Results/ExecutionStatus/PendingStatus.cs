using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Results.ExecutionStatus;

/// <summary>
/// Pending execution status - execution is queued.
/// </summary>
[TypeOption(typeof(ExecutionStatuses), "Pending", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PendingStatus : ExecutionStatusBase
{
    /// <summary>
    /// MudBlazor icon for pending status.
    /// </summary>
    public const string PendingIcon = "schedule"; // Icons.Material.Filled.Schedule

    /// <summary>
    /// Initializes a new instance of the <see cref="PendingStatus"/> class.
    /// </summary>
    public PendingStatus()
        : base(
            id: 1,
            name: "Pending",
            icon: PendingIcon,
            color: "Default",
            isTerminal: false,
            isSuccess: false,
            isInProgress: false)
    {
    }
}
