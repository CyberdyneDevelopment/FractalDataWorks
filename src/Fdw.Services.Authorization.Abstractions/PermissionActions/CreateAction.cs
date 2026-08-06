using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Permission action for creating new resources.
/// </summary>
[TypeOption(typeof(PermissionActions), "Create", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CreateAction : PermissionActionBase
{
    /// <summary>
    /// MudBlazor icon for create action.
    /// </summary>
    public const string CreateIcon = "add"; // Icons.Material.Filled.Add

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAction"/> class.
    /// </summary>
    public CreateAction()
        : base(
            id: 3,
            name: "Create",
            icon: CreateIcon,
            color: "Success",
            description: "Create new resources",
            isWriteAction: true,
            isDestructive: false)
    {
    }
}