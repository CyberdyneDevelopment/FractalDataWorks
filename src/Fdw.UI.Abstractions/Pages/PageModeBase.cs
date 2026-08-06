using Fdw.Collections;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Base class for page modes.
/// Inherit from this class and apply [TypeOption] attribute to create page modes.
/// </summary>
public abstract class PageModeBase : TypeOptionBase<int, PageModeBase>, IPageMode
{
    /// <summary>
    /// Creates a new page mode.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="name">Unique name used for lookup.</param>
    /// <param name="label">Display label.</param>
    /// <param name="icon">Icon to display.</param>
    /// <param name="isEditable">Whether fields are editable in this mode.</param>
    /// <param name="isCreateMode">Whether this mode is for creating new entities.</param>
    protected PageModeBase(
        int id,
        string name,
        string label,
        string icon,
        bool isEditable,
        bool isCreateMode)
        : base(id, name)
    {
        Label = label;
        Icon = icon;
        IsEditable = isEditable;
        IsCreateMode = isCreateMode;
    }

    /// <inheritdoc />
    public string Label { get; }

    /// <inheritdoc />
    public string Icon { get; }

    /// <inheritdoc />
    public bool IsEditable { get; }

    /// <inheritdoc />
    public bool IsCreateMode { get; }

    /// <inheritdoc />
    public abstract string GetTitlePrefix(string entityDisplayName);
}
