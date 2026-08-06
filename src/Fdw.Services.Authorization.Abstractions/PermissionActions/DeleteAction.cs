using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Permission action for deleting resources.
/// </summary>
[TypeOption(typeof(PermissionActions), "Delete", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeleteAction : PermissionActionBase
{
    /// <summary>
    /// MudBlazor icon for delete action.
    /// </summary>
    public const string DeleteIcon = "delete"; // Icons.Material.Filled.Delete

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAction"/> class.
    /// </summary>
    public DeleteAction()
        : base(
            id: 5,
            name: "Delete",
            icon: DeleteIcon,
            color: "Error",
            description: "Remove resources permanently",
            isWriteAction: true,
            isDestructive: true)
    {
    }
}