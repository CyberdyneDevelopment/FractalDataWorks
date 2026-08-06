using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Permission action for managing resources.
/// </summary>
[TypeOption(typeof(PermissionActions), "Manage", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ManageAction : PermissionActionBase
{
    /// <summary>
    /// MudBlazor icon for manage action.
    /// </summary>
    public const string ManageIcon = "settings"; // Icons.Material.Filled.Settings

    /// <summary>
    /// Initializes a new instance of the <see cref="ManageAction"/> class.
    /// </summary>
    public ManageAction()
        : base(
            id: 10,
            name: "Manage",
            icon: ManageIcon,
            color: "Primary",
            description: "Configure and manage resource settings",
            isWriteAction: true,
            isDestructive: false)
    {
    }
}