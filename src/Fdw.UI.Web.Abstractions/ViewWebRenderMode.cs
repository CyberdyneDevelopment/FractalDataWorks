using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Read-only display render mode.
/// </summary>
// Why: pure TypeOption leaf — literal constructor values only, no logic to test.
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RenderModes), "View", RestrictToCurrentCompilation = true)]
public sealed class ViewWebRenderMode : WebRenderModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewWebRenderMode"/> class.
    /// </summary>
    public ViewWebRenderMode() : base(1, "View", "View Mode", "Read-only display") { }
}