using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Show view and edit simultaneously render mode.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RenderModes), "Both", RestrictToCurrentCompilation = true)]
public sealed class BothWebRenderMode : WebRenderModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BothWebRenderMode"/> class.
    /// </summary>
    public BothWebRenderMode() : base(4, "Both", "Both Mode", "View and edit simultaneously") { }
}