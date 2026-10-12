using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Permission action for updating existing resources.
/// </summary>
[TypeOption(typeof(PermissionActions), "Update", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpdateAction : PermissionActionBase
{
    /// <summary>
    /// Material Symbols icon name for update action.
    /// </summary>
    public const string UpdateIcon = "edit";

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAction"/> class.
    /// </summary>
    public UpdateAction()
        : base(
            name: "Update",
            icon: UpdateIcon,
            color: "Success",
            description: "Update existing resources",
            isWriteAction: true,
            isDestructive: false)
    {
    }
}