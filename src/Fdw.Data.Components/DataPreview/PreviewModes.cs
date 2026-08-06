namespace Fdw.Data.Components.DataPreview;

using Fdw.Collections;
using Fdw.Collections.Attributes;

/// <summary>
/// TypeCollection for data preview modes.
/// Use <c>ByName</c> for O(1) lookup from a string value.
/// </summary>
[TypeCollection(typeof(PreviewModeBase), typeof(IPreviewMode), typeof(PreviewModes))]
public abstract partial class PreviewModes : TypeCollectionBase<PreviewModeBase, IPreviewMode> { }
