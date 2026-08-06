using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.RenderModeOptions;

/// <summary>
/// Edit mode (editable inputs).
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RenderModes), "Edit", RestrictToCurrentCompilation = true)]
public sealed class EditRenderMode : RenderModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditRenderMode"/> class.
    /// </summary>
    public EditRenderMode() : base(1, "Edit", allowsEditing: true, showsView: false) { }
}
