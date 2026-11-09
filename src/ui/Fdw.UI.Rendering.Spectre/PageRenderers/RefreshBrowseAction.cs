using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Reload the active column's items.</summary>
[TypeOption(typeof(BrowseActions), "Refresh")]
public sealed class RefreshBrowseAction : BrowseActionBase
{
    /// <summary>Initializes a new instance of the <see cref="RefreshBrowseAction"/> class.</summary>
    public RefreshBrowseAction()
        : base(3, "Refresh", "Refresh")
    {
    }
}
