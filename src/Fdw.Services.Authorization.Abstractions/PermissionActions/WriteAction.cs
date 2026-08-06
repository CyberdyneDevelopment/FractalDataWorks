using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Permission action for modifying resources.
/// </summary>
[TypeOption(typeof(PermissionActions), "Write", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class WriteAction : PermissionActionBase
{
    /// <summary>
    /// MudBlazor icon for write action.
    /// </summary>
    public const string WriteIcon = "edit"; // Icons.Material.Filled.Edit

    /// <summary>
    /// Initializes a new instance of the <see cref="WriteAction"/> class.
    /// </summary>
    public WriteAction()
        : base(
            id: 2,
            name: "Write",
            icon: WriteIcon,
            color: "Success",
            description: "Modify existing resources",
            isWriteAction: true,
            isDestructive: false)
    {
    }
}