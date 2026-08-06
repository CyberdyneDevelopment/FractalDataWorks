using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Permission action for executing operations.
/// </summary>
[TypeOption(typeof(PermissionActions), "Execute", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecuteAction : PermissionActionBase
{
    /// <summary>
    /// MudBlazor icon for execute action.
    /// </summary>
    public const string ExecuteIcon = "play_arrow"; // Icons.Material.Filled.PlayArrow

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteAction"/> class.
    /// </summary>
    public ExecuteAction()
        : base(
            id: 6,
            name: "Execute",
            icon: ExecuteIcon,
            color: "Warning",
            description: "Run pipelines, schedules, or workflows",
            isWriteAction: false,
            isDestructive: false)
    {
    }
}