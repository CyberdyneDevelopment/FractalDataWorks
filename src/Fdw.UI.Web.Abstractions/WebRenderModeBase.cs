using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Base class for render mode types.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class WebRenderModeBase : TypeOptionBase<WebRenderModeBase>, IRenderMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebRenderModeBase"/> class.
    /// </summary>
    /// <param name="name">The unique identifier for the render mode.</param>
    /// <param name="displayName">The name of the render mode.</param>
    /// <param name="description">The display name for the render mode.</param>
    protected WebRenderModeBase(string name, string displayName, string description)
        : base(name, $"RenderModes:{name}", displayName, description, "RenderModes")
    {
    }
}