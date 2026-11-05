using Fdw.Collections;

namespace Fdw.UI.Rendering.Spectre.PageRenderers;

/// <summary>Base of the <see cref="BrowseActions"/> options: each names an action and the menu text it is chosen by.</summary>
public abstract class BrowseActionBase : TypeOptionBase<BrowseActionBase>, IBrowseAction
{
    /// <summary>Initializes a new instance of the <see cref="BrowseActionBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection; it is also the action's position in the browse menu.</param>
    /// <param name="name">The action's name.</param>
    /// <param name="menuLabel">The text the browse prompt shows for the action, or null when it is not a menu entry.</param>
    protected BrowseActionBase(int id, string name, string? menuLabel)
        : base(id, name)
    {
        MenuLabel = menuLabel;
    }

    /// <inheritdoc />
    public string? MenuLabel { get; }
}
