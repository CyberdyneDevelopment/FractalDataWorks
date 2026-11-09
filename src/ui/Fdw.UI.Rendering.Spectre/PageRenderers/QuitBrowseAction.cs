using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Quit the browse session.</summary>
[TypeOption(typeof(BrowseActions), "Quit")]
public sealed class QuitBrowseAction : BrowseActionBase
{
    /// <summary>Initializes a new instance of the <see cref="QuitBrowseAction"/> class.</summary>
    public QuitBrowseAction()
        : base(4, "Quit", "Quit")
    {
    }
}
