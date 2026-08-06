using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.RenderModeOptions;

/// <summary>
/// Display-only mode (read-only).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RenderModes), "View", RestrictToCurrentCompilation = true)]
public sealed class ViewRenderMode : RenderModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewRenderMode"/> class.
    /// </summary>
    public ViewRenderMode() : base(0, "View", allowsEditing: false, showsView: true) { }
}
