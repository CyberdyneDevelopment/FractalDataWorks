using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Permission action for viewing resources.
/// </summary>
[TypeOption(typeof(PermissionActions), "Read", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ReadAction : PermissionActionBase
{
    /// <summary>
    /// Material Symbols icon name for read action.
    /// </summary>
    public const string ReadIcon = "visibility";

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAction"/> class.
    /// </summary>
    public ReadAction()
        : base(
            name: "Read",
            icon: ReadIcon,
            color: "Info",
            description: "View resource data",
            isWriteAction: false,
            isDestructive: false)
    {
    }
}