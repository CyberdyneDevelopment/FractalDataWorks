using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.RenderModeOptions;

/// <summary>
/// Both view and edit side-by-side.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RenderModes), "Both", RestrictToCurrentCompilation = true)]
public sealed class BothRenderMode : RenderModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BothRenderMode"/> class.
    /// </summary>
    public BothRenderMode() : base(2, "Both", allowsEditing: true, showsView: true) { }
}
