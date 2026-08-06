using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Results.ExecutionStatus;

/// <summary>
/// Cancelled execution status - execution was cancelled.
/// </summary>
[TypeOption(typeof(ExecutionStatuses), "Cancelled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CancelledStatus : ExecutionStatusBase
{
    /// <summary>
    /// MudBlazor icon for cancelled status.
    /// </summary>
    public const string CancelledIcon = "cancel"; // Icons.Material.Filled.Cancel

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelledStatus"/> class.
    /// </summary>
    public CancelledStatus()
        : base(
            id: 5,
            name: "Cancelled",
            icon: CancelledIcon,
            color: "Warning",
            isTerminal: true,
            isSuccess: false,
            isInProgress: false)
    {
    }
}
