using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components;

/// <summary>
/// Abstract base for application layouts, enabling pages to call layout features
/// without coupling to a specific application skin.
/// </summary>
public abstract class MainLayoutBase : LayoutComponentBase
{
    /// <summary>Removes default page padding when set to <see langword="true"/>.</summary>
    public virtual void SetNoPadding(bool noPadding) { }

    /// <summary>Toggles fullscreen mode when set to <see langword="true"/>.</summary>
    public virtual void SetFullscreen(bool fullscreen) { }
}
