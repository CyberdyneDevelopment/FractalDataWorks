using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Permission action for administrative operations.
/// </summary>
[TypeOption(typeof(PermissionActions), "Admin", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AdminAction : PermissionActionBase
{
    /// <summary>
    /// MudBlazor icon for admin action.
    /// </summary>
    public const string AdminIcon = "admin_panel_settings"; // Icons.Material.Filled.AdminPanelSettings

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminAction"/> class.
    /// </summary>
    public AdminAction()
        : base(
            id: 8,
            name: "Admin",
            icon: AdminIcon,
            color: "Primary",
            description: "Full administrative access",
            isWriteAction: true,
            isDestructive: true)
    {
    }
}