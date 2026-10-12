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
    /// Material Symbols icon name for approve action.
    /// </summary>
    public const string ApproveIcon = "check_circle";

    /// <summary>
    /// Initializes a new instance of the <see cref="ApproveAction"/> class.
    /// </summary>
    public ApproveAction()
        : base(
            name: "Approve",
            icon: ApproveIcon,
            color: "Tertiary",
            description: "Approve changes or requests",
            isWriteAction: true,
            isDestructive: false)
    {
    }
}