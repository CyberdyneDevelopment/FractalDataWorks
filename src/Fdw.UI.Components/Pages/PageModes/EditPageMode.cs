using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Edit mode - editing an existing entity.
/// </summary>
[TypeOption(typeof(PageModes), "Edit", RestrictToCurrentCompilation = true)]
public sealed class EditPageMode : PageModeBase
{
    /// <summary>
    /// Creates the edit page mode.
    /// </summary>
    public EditPageMode() : base(
        id: 3,
        name: "Edit",
        label: "Edit",
        icon: "✏",
        isEditable: true,
        isCreateMode: false)
    {
    }

    /// <inheritdoc />
    public override string GetTitlePrefix(string entityDisplayName) => $"Edit {entityDisplayName}";
}
