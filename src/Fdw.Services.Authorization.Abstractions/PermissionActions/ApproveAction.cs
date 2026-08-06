using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Permission action for approving items.
/// </summary>
[TypeOption(typeof(PermissionActions), "Approve", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ApproveAction : PermissionActionBase
{
    /// <summary>
    /// MudBlazor icon for approve action.
    /// </summary>
    public const string ApproveIcon = "check_circle"; // Icons.Material.Filled.CheckCircle

    /// <summary>
    /// Initializes a new instance of the <see cref="ApproveAction"/> class.
    /// </summary>
    public ApproveAction()
        : base(
            id: 9,
            name: "Approve",
            icon: ApproveIcon,
            color: "Tertiary",
            description: "Approve changes or requests",
            isWriteAction: true,
            isDestructive: false)
    {
    }
}