using Fdw.Collections;

namespace Fdw.UI.Themes;

/// <summary>
/// Abstract base class for icon sets.
/// Inherit from this class and apply [TypeOption] attribute to create custom icon sets.
/// </summary>
public abstract class IconSetBase : TypeOptionBase<int, IconSetBase>, IIconSet
{
    /// <summary>
    /// Creates a new icon set.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="name">Display name.</param>
    protected IconSetBase(int id, string name) : base(id, name) { }

    /// <inheritdoc />
    public abstract string SelectedIndicator { get; }

    /// <inheritdoc />
    public abstract string UnselectedIndicator { get; }

    /// <inheritdoc />
    public abstract string CheckedIndicator { get; }

    /// <inheritdoc />
    public abstract string UncheckedIndicator { get; }

    /// <inheritdoc />
    public abstract string RequiredIndicator { get; }

    /// <inheritdoc />
    public abstract string SuccessIcon { get; }

    /// <inheritdoc />
    public abstract string ErrorIcon { get; }

    /// <inheritdoc />
    public abstract string WarningIcon { get; }

    /// <inheritdoc />
    public abstract string InfoIcon { get; }

    /// <inheritdoc />
    public abstract string ExpandedIcon { get; }

    /// <inheritdoc />
    public abstract string CollapsedIcon { get; }

    /// <inheritdoc />
    public abstract string LoadingIcon { get; }
}
