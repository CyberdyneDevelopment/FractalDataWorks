using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Editable input render mode.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RenderModes), "Edit", RestrictToCurrentCompilation = true)]
public sealed class EditWebRenderMode : WebRenderModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditWebRenderMode"/> class.
    /// </summary>
    public EditWebRenderMode() : base(2, "Edit", "Edit Mode", "Editable input") { }
}