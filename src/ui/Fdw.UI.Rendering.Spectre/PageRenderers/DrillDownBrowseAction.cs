using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Drill into the selected item (load the next column). Chosen by selecting an item, so it is not a menu entry.</summary>
[TypeOption(typeof(BrowseActions), "DrillDown")]
public sealed class DrillDownBrowseAction : BrowseActionBase
{
    /// <summary>Initializes a new instance of the <see cref="DrillDownBrowseAction"/> class.</summary>
    public DrillDownBrowseAction()
        : base(1, "DrillDown", null)
    {
    }
}
