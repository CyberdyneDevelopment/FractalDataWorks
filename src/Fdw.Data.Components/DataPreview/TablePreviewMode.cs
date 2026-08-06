namespace Fdw.Data.Components.DataPreview;

using Fdw.Collections.Attributes;

/// <summary>Preview data from a raw connection table or view.</summary>
[TypeOption(typeof(PreviewModes), "Table")]
public sealed class TablePreviewMode : PreviewModeBase
{
    /// <summary>Initializes a new instance of the <see cref="TablePreviewMode"/> class.</summary>
    public TablePreviewMode() : base(2, "Table") { }
}
