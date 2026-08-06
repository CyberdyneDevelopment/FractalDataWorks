using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Results.ExecutionStatus;

/// <summary>
/// Succeeded execution status - execution completed successfully.
/// </summary>
[TypeOption(typeof(ExecutionStatuses), "Succeeded", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SucceededStatus : ExecutionStatusBase
{
    /// <summary>
    /// MudBlazor icon for succeeded status.
    /// </summary>
    public const string SucceededIcon = "check_circle"; // Icons.Material.Filled.CheckCircle

    /// <summary>
    /// Initializes a new instance of the <see cref="SucceededStatus"/> class.
    /// </summary>
    public SucceededStatus()
        : base(
            id: 3,
            name: "Succeeded",
            icon: SucceededIcon,
            color: "Success",
            isTerminal: true,
            isSuccess: true,
            isInProgress: false)
    {
    }
}
