using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Permission action for browsing/navigating resources.
/// </summary>
[TypeOption(typeof(PermissionActions), "Browse", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BrowseAction : PermissionActionBase
{
    /// <summary>
    /// Material Symbols icon name for browse action.
    /// </summary>
    public const string BrowseIcon = "folder_open";

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowseAction"/> class.
    /// </summary>
    public BrowseAction()
        : base(
            name: "Browse",
            icon: BrowseIcon,
            color: "Info",
            description: "Navigate and explore resource hierarchy",
            isWriteAction: false,
            isDestructive: false)
    {
    }
}