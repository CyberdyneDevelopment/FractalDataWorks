using Fdw.Collections;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// Base class for UI renderer types.
/// </summary>
/// <remarks>
/// Inherit from this class and apply [TypeOption] attribute to create custom renderer types.
/// </remarks>
public abstract class UIRendererBase : TypeOptionBase<int, UIRendererBase>, IUIRendererType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UIRendererBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The name of this renderer type.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    protected UIRendererBase(int id, string name, string displayName, string description)
        : base(id, name)
    {
        DisplayName = displayName;
        Description = description;
    }

    /// <inheritdoc />
    public new string DisplayName { get; }

    /// <inheritdoc />
    public new string Description { get; }

    /// <inheritdoc />
    public abstract bool SupportsInteractiveMode { get; }

    /// <inheritdoc />
    public abstract bool SupportsAnsiColors { get; }

    /// <inheritdoc />
    public abstract bool SupportsFocusManagement { get; }

    /// <inheritdoc />
    public abstract bool SupportsHotReload { get; }
}
