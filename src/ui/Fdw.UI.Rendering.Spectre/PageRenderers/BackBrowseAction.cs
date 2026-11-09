using Fdw.Collections.Attributes;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Step back to the parent column.</summary>
[TypeOption(typeof(BrowseActions), "Back")]
public sealed class BackBrowseAction : BrowseActionBase
{
    /// <summary>Initializes a new instance of the <see cref="BackBrowseAction"/> class.</summary>
    public BackBrowseAction()
        : base(2, "Back", "← Back")
    {
    }
}
