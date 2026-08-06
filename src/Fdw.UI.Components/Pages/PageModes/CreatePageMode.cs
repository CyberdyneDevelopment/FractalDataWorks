using Fdw.Collections.Attributes;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Create mode - creating a new entity.
/// </summary>
[TypeOption(typeof(PageModes), "Create", RestrictToCurrentCompilation = true)]
public sealed class CreatePageMode : PageModeBase
{
    /// <summary>
    /// Creates the create page mode.
    /// </summary>
    public CreatePageMode() : base(
        id: 2,
        name: "Create",
        label: "Create",
        icon: "+",
        isEditable: true,
        isCreateMode: true)
    {
    }

    /// <inheritdoc />
    public override string GetTitlePrefix(string entityDisplayName) => $"New {entityDisplayName}";
}
