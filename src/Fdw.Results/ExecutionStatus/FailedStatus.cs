using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Results.ExecutionStatus;

/// <summary>
/// Failed execution status - execution failed with errors.
/// </summary>
[TypeOption(typeof(ExecutionStatuses), "Failed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedStatus : ExecutionStatusBase
{
    /// <summary>
    /// MudBlazor icon for failed status.
    /// </summary>
    public const string FailedIcon = "error"; // Icons.Material.Filled.Error

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedStatus"/> class.
    /// </summary>
    public FailedStatus()
        : base(
            id: 4,
            name: "Failed",
            icon: FailedIcon,
            color: "Error",
            isTerminal: true,
            isSuccess: false,
            isInProgress: false)
    {
    }
}
