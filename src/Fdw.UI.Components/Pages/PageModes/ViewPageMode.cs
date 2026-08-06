using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// View mode - displaying an existing entity (read-only).
/// </summary>
[TypeOption(typeof(PageModes), "View", RestrictToCurrentCompilation = true)]
public sealed class ViewPageMode : PageModeBase
{
    /// <summary>
    /// Creates the view page mode.
    /// </summary>
    public ViewPageMode() : base(
        id: 1,
        name: "View",
        label: "View",
        icon: "👁",
        isEditable: false,
        isCreateMode: false)
    {
    }

    /// <inheritdoc />
    public override string GetTitlePrefix(string entityDisplayName) => entityDisplayName;
}
